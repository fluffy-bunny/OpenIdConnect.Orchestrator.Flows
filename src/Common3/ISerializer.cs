﻿namespace Common
{
    public interface ISerializer
    {
        string Serialize<T>(T obj, bool indent = false) where T : class;
        T Deserialize<T>(string text) where T : class;
    }
}
