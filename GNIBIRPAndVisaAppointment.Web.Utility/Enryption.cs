﻿using System;
using System.IO;
using System.Linq;

namespace GNIBIRPAndVisaAppointment.Web.Utility
{
    public static class Encryption
    {
        public static string ToMD5(this string input)
        {
            using(var md5 = System.Security.Cryptography.MD5.Create())
            {
                var inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                var md5Bytes = md5.ComputeHash(inputBytes);
                return string.Join(string.Empty, md5Bytes.Select(md5Byte => md5Byte.ToString("X2")));
            }
        }

        public static string ToMD5(this Stream fileStream)
        {
            using(var md5 = System.Security.Cryptography.MD5.Create())
            {
                fileStream.Position = 0;
                var md5Bytes = md5.ComputeHash(fileStream);
                return string.Join(string.Empty, md5Bytes.Select(md5Byte => md5Byte.ToString("X2")));
            }
        }
    }
}
