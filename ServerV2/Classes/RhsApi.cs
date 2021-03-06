﻿using Library;
using Models;
using Newtonsoft.Json;
using Library;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Helpers;

namespace ServerV2.Classes
{
    public class RhsApi
    {
        //private static string publicKey;
        //private static string privateKey;
        private static readonly string dateformat = "MMM ddd d HH:mm:ss yyyy";// "Y-m-d H:i:s";

        public static Keys Generate()
        {
            return new Keys()
            {
                Public = Security.SHA1(Security.mt_rand_str(40)),
                Private = Security.SHA1(Security.mt_rand_str(40))
            };
        }

        public static string GetFileContents(string path, string file)
        {
            string result = null;
            var fullName = GetFile(path, file);
            try
            {
                result = System.IO.File.ReadAllText(fullName);
            }
            catch { }
            return result;
        }

        public static string GetFileContents(string fullpath)
        {
            return System.IO.File.ReadAllText(fullpath);
        }

        public static string GetFile(string path, string file)
        {
            // Some browsers send file names with full path. We only care about the file name.
            return Path.Combine(HttpContext.Current.Server.MapPath(path), Path.GetFileName(file));
        }

        private static Keys GetKeys()
        {
            return JsonConvert.DeserializeObject<Keys>(GetFileContents("~/App_Data", "keys.json"));
        }

        public static AuthResult Authorize(AuthData data)
        {
            Keys keys = GetKeys();
            if (data != null && keys != null && keys.Public.Equals(data.PublicKey))
            {
                var privateKey = keys.Private;
                var hashCheck = General.Sha1Hash(data.Data + privateKey + data.PublicKey);
                if (hashCheck != null && hashCheck.Equals(data.Hash))
                {
                    var newToken = General.Sha1Hash(privateKey + hashCheck + GetDateTimeFormatted());
                    var computersJsonFile = GetFile("~/App_Data/", "computers.json");
                    var computers = new List<ComputerData>();
                    try
                    {
                        computers = JsonConvert.DeserializeObject<List<ComputerData>>(GetFileContents(computersJsonFile));
                    }
                    catch { }
                    var computerData = GetComputerData(data);
                    var computer = computers.Where(c => c.Hash == computerData.Hash).FirstOrDefault();
                    if (computer == null)
                    {
                        computerData.IsOnline = true;
                        computers.Add(computerData);
                    }
                    else
                    {
                        computer.IsOnline = true;
                    }
                    var computersJson = JsonConvert.SerializeObject(computers);
                    try
                    {
                        File.WriteAllText(computersJsonFile, computersJson);
                    }
                    catch { }
                    return new AuthResult()
                    {
                        Token = newToken,
                        IpExternal = data.IpExternal
                    };
                }
            }
            return null;
        }

        public static bool DeAuthorize(AuthData data)
        {
            Keys keys = GetKeys();
            if (data != null && keys != null && keys.Public.Equals(data.PublicKey))
            {
                var privateKey = keys.Private;
                var hashCheck = General.Sha1Hash(data.Data + privateKey + data.PublicKey);
                if (hashCheck != null && hashCheck.Equals(data.Hash))
                {
                    var computersJsonFile = GetFile("~/App_Data/", "computers.json");
                    var computers = new List<ComputerData>();
                    try
                    {
                        computers = JsonConvert.DeserializeObject<List<ComputerData>>(GetFileContents(computersJsonFile));
                    }
                    catch { }
                    var computerData = GetComputerData(data);
                    var computer = computers.Find(x => x.Hash == computerData.Hash);
                    if (computer != null)
                    {
                        computer.IsOnline = false;
                        //computers.RemoveAt(computers.IndexOf(computer));
                        var computersJson = JsonConvert.SerializeObject(computers);
                        System.Threading.Thread.Sleep(250);
                        try
                        {
                            File.WriteAllText(computersJsonFile, computersJson);
                        }
                        catch { }
                    }
                    return true;
                }
            }
            return false;
        }

        private static ComputerData GetComputerData(AuthData data)
        {
            ComputerData computerData = null;
            if (data != null)
            {
                computerData = new ComputerData()
                {
                    Name = data.HostName,
                    IpExternal = data.IpExternal,
                    IpInternal = data.IpInternal,
                    LastActive = DateTime.Now,
                    FileUploaded = null,
                    BytesUploaded = 0,
                    Hash = null,
                    IsOnline = false
                };
                // hash value needs to be calculated after constructor
                computerData.Hash = Transmitter.GetComputerHash(computerData);
            }
            return computerData;
        }

        public static bool UpdateLastActive(AuthData data)
        {
            Keys keys = GetKeys();
            if (data != null && keys != null && keys.Public.Equals(data.PublicKey))
            {
                var privateKey = keys.Private;
                var hashCheck = General.Sha1Hash(data.Data + privateKey + data.PublicKey);
                if (hashCheck.Equals(data.Hash))
                {
                    //var newToken = General.Sha1Hash(privateKey + hashCheck + GetDateTimeFormatted());
                    var computerData = new ComputerData()
                    {
                        Name = data.HostName,
                        IpExternal = data.IpExternal,
                        IpInternal = data.IpInternal,
                        LastActive = DateTime.Now,
                        FileUploaded = null,
                        BytesUploaded = 0,
                        Hash = null
                    };
                    computerData.Hash = Transmitter.GetComputerHash(computerData);
                    return UpdateLastActive(computerData.Hash);
                }
            }
            else
            {
                if (data != null && !string.IsNullOrEmpty(data.Hash))
                {
                    return UpdateLastActive(data.Hash);
                }
            }
            return false;
        }

        public static bool UpdateLastActive(string computerHash)
        {
            var computersJsonFile = GetFile("~/App_Data/", "computers.json");
            var computers = new List<ComputerData>();
            try
            {
                computers = JsonConvert.DeserializeObject<List<ComputerData>>(GetFileContents(computersJsonFile));
            }
            catch { }
            var computer = computers.Find(c => c.Hash == computerHash);
            if (computer != null)
            {
                computer.LastActive = DateTime.Now;
            }
            var computersJson = JsonConvert.SerializeObject(computers);
            System.Threading.Thread.Sleep(250);
            File.WriteAllText(computersJsonFile, computersJson);
            return true;
        }

        // php time()
        private static double GetTime()
        {
            return (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
        }

        public static string GetDateTime(double time)
        {
            return DateTime.Now.ToString(dateformat);// new DateTime((long)GetTime()).ToString(dateformat);// DateTime.Now.ToString(dateformat);
            //return date(RHS_API::$dateformat, $time);
        }

        public static string GetDateTimeFormatted()
        {
            return GetDateTime(GetTime());
        }

        public static string UploadImage(ImageData data)
        {
            try
            {
                var path = GetPath("~/App_Data/DataFromClient", data.ComputerHash);
                var file = GetFile(path, data.FileName);
                var content = Convert.FromBase64String(data.Image);
                File.WriteAllBytes(file, content);
                if (data.Image.Length > 0)
                {
                    return file;
                }
                return null;
            }
            catch { }
            return null;
        }

        public static int? UploadFile(FileData data)
        {
            try
            {
                var path = GetPath("~/App_Data/DataFromClient", data.ComputerHash);
                var file = GetFile(path, data.FileNameWithExtension);
                byte[] bytes = Convert.FromBase64String(data.Data);
                File.WriteAllBytes(file, bytes);
                return bytes.Length;
            }
            catch { }
            return null;
        }

        static string GetPath(string basedir, string computerHash)
        {
            var path = !string.IsNullOrEmpty(computerHash) ? Path.Combine(basedir, computerHash) : basedir;
            var realPath = HttpContext.Current.Request.MapPath(path);
            if (!Directory.Exists(realPath))
            {
                try
                {
                    Directory.CreateDirectory(realPath);
                }
                catch { }
            }
            return path;
        }

        public static string DownloadFile(string file)
        {
            try
            {
                var result = File.ReadAllBytes(GetFile("~/App_Data/DataFromHost/", file));
                return Convert.ToBase64String(result);
            }
            catch { }
            return null;
        }

        public static Settings GetSettings()
        {
            return JsonConvert.DeserializeObject<Settings>(GetFileContents("~/App_Data", "settings.json"));
        }

        internal static int? SaveSettings(Settings settingsEncoded)
        {
            try
            {
                var file = GetFile("~/App_Data/", "settings.json");
                byte[] bytes = Encoding.Default.GetBytes(JsonConvert.SerializeObject(settingsEncoded));
                File.WriteAllBytes(file, bytes);
                return bytes.Length;
            }
            catch { }
            return null;
        }

        public static string GetComputerHash(string computerName)
        {
            var data = JsonConvert.DeserializeObject<List<ComputerData>>(GetFileContents("~/App_Data", "computers.json"));
            var computer = data.Where(c => c.Name == computerName).FirstOrDefault();
            if (computer != null)
            {
                return Transmitter.GetComputerHash(computer);
            }
            return null;
        }

        //private static ResetToken(bool datetimeValid, string tokenValue) {
        //    var dateInPast = GetDateTimeFormatted();
        //    dateInPast.
        //    $dateInPast->sub(new DateInterval($datetimeValid));
        //    return DB::update('CWM_ApiKeySession', array('LastAccess' => $dateInPast, 'TokenValue' => ''), "TokenValue=%?", $tokenValue);
        //}

        /*

        public static function deAuthorize($tokenValue) {
            return RHS_API::resetToken("P1D", $tokenValue);
        }

        private static function resetToken($datetimeValid, $tokenValue) {
            $dateInPast = new DateTime(RHS_API::getDateTime(time()));
            $dateInPast->sub(new DateInterval($datetimeValid));
            return DB::update('CWM_ApiKeySession', array('LastAccess' => $dateInPast, 'TokenValue' => ''), "TokenValue=%?", $tokenValue);
        }

        public static function isTokenValid($newTokenValue) {
            $datetimeValid = "P1D"; // valid for 1 day
            $result = false;
            $lastAccess = RHS_API::getDateTime(strtotime(DB::queryOneField('LastAccess', 'SELECT * FROM CWM_ApiKeySession WHERE TokenValue=%?', $newTokenValue)));
            $lastAccess = new DateTime($lastAccess);
            $lastAccess->add(new DateInterval($datetimeValid));
            $result = new DateTime(RHS_API::getDateTime(time())) < $lastAccess;
            // if not valid, update the date to be in the past
            if(!$result) {
                RHS_API::resetToken($datetimeValid, $newTokenValue);
            }
            return $result;
        }

        public static function formatDateTime($datetime) {
            return $datetime->format(RHS_API::$dateformat);
        }
        */
        public static string getAsJson(object data)
        {
            //header('content-type: application/json; charset=utf-8');
            return Json.Encode(data);
        }
    }
}