/**
 * Copyright (c) 2014-present, Facebook, Inc. All rights reserved
 *
 * You are hereby granted a non-exclusive, worldwide, royalty-free license to use,
 * copy, modify, and distribute this software in source code or binary form for use
 * in connection with the web services and APIs provided by Facebook.
 *
 * As with any software that integrates with the Facebook platform, your use of
 * this software is subject to the Facebook Developer Principles and Policies
 * [http://developers.facebook.com/policy/]. This copyright notice shall be
 * included in all copies or substantial portions of the software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
 * FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
 * COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
 * IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
 * CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

namespace Facebook.Unity.Editor
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using Facebook.Unity.Settings;
    using Google;
    using UnityEditor;
    using UnityEditor.Android;
    using UnityEngine;

    public class FacebookAndroidUtil
    {
        public const string ErrorNoSDK = "no_android_sdk";
        public const string ErrorNoKeystore = "no_android_keystore";
        public const string ErrorNoKeytool = "no_java_keytool";
        public const string ErrorNoOpenSSL = "no_openssl";
        public const string ErrorKeytoolError = "java_keytool_error";

        private static string debugKeyHash;
        private static string currentKeyHash;
        private static string setupError;

        public static bool SetupProperly
        {
            get
            {
                return DebugKeyHash != null;
            }
        }

        public static string DebugKeyHash
        {
            get
            {
                if (debugKeyHash == null)
                {
                    if (!HasAndroidSDK())
                    {
                        setupError = ErrorNoSDK;
                        return null;
                    }

                    if (!HasAndroidKeystoreFile())
                    {
                        setupError = ErrorNoKeystore;
                        return null;
                    }

                    if (!DoesCommandExist("echo \"xxx\" | openssl base64"))
                    {
                        setupError = ErrorNoOpenSSL;
                        return null;
                    }

                    if (!DoesCommandExist("keytool"))
                    {
                        setupError = ErrorNoKeytool;
                        return null;
                    }

                    debugKeyHash = GetKeyHash("androiddebugkey", DebugKeyStorePath, "android");
                }

                return debugKeyHash;
            }
        }

        public static string CurrentKeyHash
        {
            get
            {
                if (currentKeyHash == null)
                {
                    if (!HasAndroidSDK())
                    {
                        return ErrorNoSDK;
                    }

                    if(string.IsNullOrEmpty(PlayerSettings.Android.keystoreName))
                    {
                        return ErrorNoKeystore;
                    }

                    if(string.IsNullOrEmpty(PlayerSettings.Android.keyaliasName))
                    {
                        return ErrorNoKeystore;
                    }

                    if (!DoesCommandExist("echo \"xxx\" | openssl base64"))
                    {
                        return ErrorNoOpenSSL;
                    }

                    if (!DoesCommandExist("keytool"))
                    {
                        return ErrorNoKeytool;
                    }

                    currentKeyHash = GetKeyHash(PlayerSettings.Android.keyaliasName, PlayerSettings.Android.keystoreName, PlayerSettings.Android.keystorePass, PlayerSettings.Android.keyaliasPass);
                }

                return currentKeyHash;
            }
        }

        public static string SetupError
        {
            get
            {
                return setupError;
            }
        }

        private static string DebugKeyStorePath
        {
            get
            {
                if (!string.IsNullOrEmpty(FacebookSettings.AndroidKeystorePath))
                {
                    return FacebookSettings.AndroidKeystorePath;
                }
                return (Application.platform == RuntimePlatform.WindowsEditor) ?
                    System.Environment.GetEnvironmentVariable("USERPROFILE") + @"\.android\debug.keystore" :
                    System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal) + @"/.android/debug.keystore";
            }
        }

        public static bool HasAndroidSDK()
        {
            string sdkPath = GetAndroidSdkPath();
            return !string.IsNullOrEmpty(sdkPath) && System.IO.Directory.Exists(sdkPath);
        }

        public static bool HasAndroidKeystoreFile()
        {
            return System.IO.File.Exists(DebugKeyStorePath);
        }

        public static string GetAndroidSdkPath()
        {
            string sdkPath = AndroidExternalToolsSettings.sdkRootPath;

            if (string.IsNullOrEmpty(sdkPath) || EditorPrefs.GetBool("SdkUseEmbedded"))
            {
                try
                {
                    sdkPath = (string)VersionHandler.InvokeStaticMethod(typeof(BuildPipeline), "GetPlaybackEngineDirectory", new object[] { BuildTarget.Android, BuildOptions.None });
                }
                catch (Exception)
                {
                    foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        if (assembly.GetName().Name == "UnityEditor.Android.Extensions")
                        {
                            sdkPath = Path.GetDirectoryName(assembly.Location);
                            break;
                        }
                    }
                }
            }

            return sdkPath;
        }

        private static string GetKeyHash(string alias, string keyStore, string password) => GetKeyHash(alias,keyStore,password,password);
        private static string GetKeyHash(string alias, string keyStore, string keystorePassword, string aliasPassword)
        {
            var proc = new Process();
            var arguments = @"""keytool -storepass {0} -keypass {1} -exportcert -alias {2} -keystore {3} | openssl sha1 -binary | openssl base64""";
            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                proc.StartInfo.FileName = "cmd.exe";
                arguments = @"/C " + arguments;
            }
            else
            {
                proc.StartInfo.FileName = "bash";
                arguments = @"-c " + arguments;
            }

            proc.StartInfo.Arguments = string.Format(arguments, keystorePassword, aliasPassword, alias, keyStore);
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.CreateNoWindow = true;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.Start();
            var keyHash = new StringBuilder();
            while (!proc.HasExited)
            {
                keyHash.Append(proc.StandardOutput.ReadToEnd());
            }

            switch (proc.ExitCode)
            {
                case 255:
                    setupError = ErrorKeytoolError;
                    return null;
            }

            return keyHash.ToString().TrimEnd('\n');
        }

        private static bool DoesCommandExist(string command)
        {
            var proc = new Process();
            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                proc.StartInfo.FileName = "cmd.exe";
                proc.StartInfo.Arguments = @"/C" + command;
            }
            else
            {
                proc.StartInfo.FileName = "bash";
                proc.StartInfo.Arguments = @"-c " + command;
            }

            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.CreateNoWindow = true;
            proc.Start();
            proc.WaitForExit();
            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                return proc.ExitCode == 0;
            }
            else
            {
                return proc.ExitCode != 127;
            }
        }
    }
}
