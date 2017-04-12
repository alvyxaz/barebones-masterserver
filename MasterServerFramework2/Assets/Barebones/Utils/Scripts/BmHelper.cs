using System;
using Barebones.MasterServer;
using UnityEngine;
using UnityEngine.Networking;

namespace Barebones.Utils
{
    public class BmHelper
    {
        public const int MaxUnetConnections = 500;

        /// <summary>
        /// Creates a random string of a given length.
        /// Uses a substring of guid
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public static string CreateRandomString(int length)
        {
            if (length < 0) throw new ArgumentOutOfRangeException("length", "length cannot be less than zero.");
            return Guid.NewGuid().ToString().Substring(0, length);
        }

        public static string ColorToHex(Color32 color)
        {
            string hex = color.r.ToString("X2") + color.g.ToString("X2") + color.b.ToString("X2") + color.a.ToString("X2");
            return hex;
        }

        public static Color HexToColor(string hex)
        {
            byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
            return new Color32(r, g, b, 255);
        }

        public static ConnectionConfig CreateDefaultConnectionConfig()
        {
            var config = new ConnectionConfig();
            config.AddChannel(QosType.ReliableSequenced);
            config.AddChannel(QosType.Unreliable);
            return config;
        }

    }
}