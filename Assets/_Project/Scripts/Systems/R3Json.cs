using System;
using UnityEngine;

namespace Liuruan
{
    internal static class R3Json
    {
        internal static T Read<T>(TextAsset asset)
        {
            if (!asset) throw new ArgumentException("請指定 Assets/_Project/Data/r3/ 的 JSON TextAsset。");
            var value = JsonUtility.FromJson<T>(asset.text);
            if (value == null) throw new ArgumentException("JSON 根物件不可為空。");
            return value;
        }
    }
}
