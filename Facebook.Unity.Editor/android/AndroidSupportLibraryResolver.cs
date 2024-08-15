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

/**
* Copyright (c) 2014-present, Facebook, Inc. All rights reserved.
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
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;

    [InitializeOnLoad]
    public class AndroidSupportLibraryResolver
    {
        static AndroidSupportLibraryResolver()
        {
            AndroidSupportLibraryResolver.setupDependencies();
        }

        private static void setupDependencies()
        {
            // Setup the resolver using reflection as the module may not be
            // available at compile time.
            Type playServicesSupport = Google.VersionHandler.FindClass(
                "Google.JarResolver",
                "Google.JarResolver.PlayServicesSupport");

            if (playServicesSupport == null)
            {
                return;
            }

            object svcSupport = Google.VersionHandler.InvokeStaticMethod(
                playServicesSupport,
                "CreateInstance",
                new object[] {
                    "FacebookUnitySDK",
                    EditorPrefs.GetString("AndroidSdkRoot"),
                    "ProjectSettings"
                });

            AndroidSupportLibraryResolver.addSupportLibraryDependency(svcSupport, "androidx.legacy", "legacy-support-v4", "1.0.0");
            AndroidSupportLibraryResolver.addSupportLibraryDependency(svcSupport, "androidx.appcompat", "appcompat", "1.0.0");
            AndroidSupportLibraryResolver.addSupportLibraryDependency(svcSupport, "androidx.cardview", "cardview", "1.0.0");
            AndroidSupportLibraryResolver.addSupportLibraryDependency(svcSupport, "androidx.browser", "browser", "1.0.0");
        }

        public static void addSupportLibraryDependency(
            object svcSupport,
            string packagePrefix,
            String packageName,
            String version)
        {
            Google.VersionHandler.InvokeInstanceMethod(
                svcSupport,
                "DependOn",
                new object[] {
                    packagePrefix,
                    packageName,
                       version
                },
                namedArgs: new Dictionary<string, object>() {
                    {
                        "packageIds",
                        new string[] {
                            "extra-android-m2repository"
                        }
                    }
            });
        }
    }
}
