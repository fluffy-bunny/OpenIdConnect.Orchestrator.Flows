﻿using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace WebAppClient.Extensions
{
    public static class SessionExtensions
    {
        public static void Set<T>(this ISession session, string key, T value)
        {
            session.SetString(key, JsonConvert.SerializeObject(value));
        }

        public static T Get<T>(this ISession session, string key)
        {
            var value = session.GetString(key);
            return value == null ? default :
                JsonConvert.DeserializeObject<T>(value);
        }
        public static string GetSessionId(this ISession session)
        {
            if (!session.IsAvailable)
            {
                session.SetString(Guid.NewGuid().ToString(), "ensure");
            }
            return session.Id;
        }
    }
}
