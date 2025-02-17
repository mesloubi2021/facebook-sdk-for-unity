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
    using System.IO;
    using System.Text.RegularExpressions;
    using UnityEngine;
    using Facebook.Unity.Editor.iOS.Xcode;

    public class FixupFiles
    {
        public static void AddBuildFlag(string path)
        {
            string projPath = Path.Combine(path, Path.Combine("Unity-iPhone.xcodeproj", "project.pbxproj"));
            PBXProject proj = new PBXProject();
            proj.ReadFromString(File.ReadAllText(projPath));

            // When building with Unity 2019.3 and later versions, there are two Xcode targets generated.
            // The UnityFramework target will include the code and Unity-iPhone is only a thin wrapper around it.
            // With earlier versions everything is in a single Unity-iPhone target.
            var targetName = GetUnityVersionNumber() >= 201930 ? "UnityFramework" : "Unity-iPhone";
            string targetGUID = proj.TargetGuidByName(targetName);
            proj.AddBuildProperty(targetGUID, "GCC_PREPROCESSOR_DEFINITIONS", " $(inherited) FBSDKCOCOAPODS=1");
            proj.AddBuildProperty(targetGUID, "SWIFT_VERSION", "5.0");
            proj.AddFrameworkToProject(targetGUID, "Accelerate.framework", true);
            File.WriteAllText(projPath, proj.WriteToString());
        }

        protected static string Load(string fullPath)
        {
            string data;
            FileInfo projectFileInfo = new FileInfo(fullPath);
            StreamReader fs = projectFileInfo.OpenText();
            data = fs.ReadToEnd();
            fs.Close();

            return data;
        }

        protected static void Save(string fullPath, string data)
        {
            System.IO.StreamWriter writer = new System.IO.StreamWriter(fullPath, false);
            writer.Write(data);
            writer.Close();
        }

        private static int GetUnityVersionNumber()
        {
            string version = Application.unityVersion;
            string[] versionComponents = version.Split('.');

            int majorVersion = 0;
            int minorVersion = 0;

            try
            {
                if (versionComponents != null && versionComponents.Length > 0 && versionComponents[0] != null)
                {
                    majorVersion = Convert.ToInt32(versionComponents[0]);
                }

                if (versionComponents != null && versionComponents.Length > 1 && versionComponents[1] != null)
                {
                    minorVersion = Convert.ToInt32(versionComponents[1]);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error parsing Unity version number: " + e);
            }

            return (majorVersion * 100) + (minorVersion * 10);
        }
    }
}
