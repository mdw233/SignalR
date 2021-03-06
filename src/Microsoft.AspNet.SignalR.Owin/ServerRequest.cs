﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Owin.Infrastructure;

namespace Microsoft.AspNet.SignalR.Owin
{
    using WebSocketFunc = Func<IDictionary<string, object>, Task>;
    public partial class ServerRequest : IRequest
    {
        private static readonly char[] CommaSemicolon = new[] { ',', ';' };

        private Uri _url;
        private NameValueCollection _queryString;
        private NameValueCollection _headers;
        private NameValueCollection _serverVariables;
        private NameValueCollection _form;
        private bool _formInitialized;
        private object _formLock;
        private IDictionary<string, Cookie> _cookies;

        public Uri Url
        {
            get
            {
                return LazyInitializer.EnsureInitialized(
                    ref _url, () =>
                    {
                        var uriBuilder = new UriBuilder(RequestScheme, RequestHost, RequestPort, RequestPathBase + RequestPath);
                        if (!String.IsNullOrEmpty(RequestQueryString))
                        {
                            uriBuilder.Query = RequestQueryString;
                        }
                        return uriBuilder.Uri;
                    });
            }
        }


        public NameValueCollection QueryString
        {
            get
            {
                return LazyInitializer.EnsureInitialized(
                    ref _queryString, () =>
                    {
                        var collection = new NameValueCollection();
                        foreach (var kv in ParamDictionary.ParseToEnumerable(RequestQueryString))
                        {
                            collection.Add(kv.Key, kv.Value);
                        }
                        return collection;
                    });
            }
        }

        public NameValueCollection Headers
        {
            get
            {
                return LazyInitializer.EnsureInitialized(
                    ref _headers, () =>
                    {
                        var collection = new NameValueCollection();
                        foreach (var kv in RequestHeaders)
                        {
                            if (kv.Value != null)
                            {
                                for (var index = 0; index != kv.Value.Length; ++index)
                                {
                                    collection.Add(kv.Key, kv.Value[index]);
                                }
                            }
                        }
                        return collection;
                    });
            }
        }

        public NameValueCollection ServerVariables
        {
            get
            {
                return LazyInitializer.EnsureInitialized(
                    ref _serverVariables, () =>
                    {
                        var collection = new NameValueCollection();
                        var remoteIpAddress = _environment.Get<string>(OwinConstants.RemoteIpAddress);
                        if (!String.IsNullOrEmpty(remoteIpAddress))
                        {
                            collection["REMOTE_ADDR"] = remoteIpAddress;
                        }
                        return collection;
                    });
            }
        }

        public NameValueCollection Form
        {
            get
            {
                return LazyInitializer.EnsureInitialized(
                    ref _form, ref _formInitialized, ref _formLock, () =>
                    {
                        var collection = new NameValueCollection();
                        foreach (var kv in ReadForm())
                        {
                            collection.Add(kv.Key, kv.Value);
                        }
                        return collection;
                    });
            }
        }


        public IDictionary<string, Cookie> Cookies
        {
            get
            {
                return LazyInitializer.EnsureInitialized(
                    ref _cookies, () =>
                    {
                        var cookies = new Dictionary<string, Cookie>(StringComparer.OrdinalIgnoreCase);
                        var text = RequestHeaders.GetHeader("Cookie");
                        foreach (var kv in ParamDictionary.ParseToEnumerable(text, CommaSemicolon))
                        {
                            if (!cookies.ContainsKey(kv.Key))
                            {
                                cookies.Add(kv.Key, new Cookie(kv.Key, kv.Value));
                            }
                        }
                        return cookies;
                    });
            }
        }

        public IPrincipal User
        {
            get { return _environment.Get<IPrincipal>(OwinConstants.User); }
        }


        public IDictionary<string, object> Items
        {
            get;
            private set;
        }

        public Task AcceptWebSocketRequest(Func<IWebSocket, Task> callback)
        {
#if NET45
            var accept = _environment.Get<Action<IDictionary<string, object>, WebSocketFunc>>(OwinConstants.WebSocketAccept);
            if (accept == null)
            {
                return TaskAsyncHelper.FromError(new InvalidOperationException("Not a web socket request"));
            }

            var options = new Dictionary<string, object>();
            var handler = new OwinWebSocketHandler(callback);
            accept(options, handler.ProcessRequestAsync);
            return TaskAsyncHelper.Empty;
#else
            throw new NotImplementedException();
#endif
        }
    }
}
