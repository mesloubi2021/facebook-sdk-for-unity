﻿/**
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

namespace Facebook.Unity.Windows
{
    using System;
    using System.Collections.Generic;

    using UnityEngine;

    internal class WindowsWrapper : IWindowsWrapper
    {

        private fbg.InitResult result;
        private string userID;

        public WindowsWrapper()
        {
        }

        public bool Init(string appId, string clientToken)
        {
            WindowsOptions options = new WindowsOptions(clientToken);
            this.result = fbg.Globals.init(appId, JsonUtility.ToJson(options));
            return result == fbg.InitResult.Success;
        }

        public void LogInWithScopes(IEnumerable<string> scope, string callbackId, CallbackManager callbackManager)
        {
            Dictionary<string, object> result = new Dictionary<string, object>() { { Constants.CallbackIdKey, callbackId } };

            try
            {
                List<fbg.LoginScope> loginScopes = this.ConvertToFbgLoginScope(scope);

                fbg.Globals.loginWithScopes(loginScopes.ToArray(), (accessToken) =>
                {
                    var perms = accessToken.Permissions.ConvertAll<string>(value => value.ToString());
                    var dataTime = Utilities.FromTimestamp((int)accessToken.Expiration);
                    this.userID = accessToken.UserID.ToString();
                    result["WindowsCurrentAccessToken"] = new AccessToken(accessToken.Token, userID, dataTime, perms, null, "fb.gg");

                    callbackManager.OnFacebookResponse(new LoginResult((new ResultContainer(result))));
                }, (error) =>
                {
                    string msg = "ERROR: " + error.Message + ",";
                    msg += "InnerErrorCode: " + error.InnerErrorCode.ToString() + ",";
                    msg += "InnerErrorMessage: " + error.InnerErrorMessage + ",";
                    msg += "InnerErrorSubcode: " + error.InnerErrorSubcode.ToString() + ",";
                    msg += "InnerErrorTraceId: " + error.InnerErrorTraceId;

                    result[Constants.ErrorKey] = msg;
                    callbackManager.OnFacebookResponse(new LoginResult((new ResultContainer(result))));
                });
            }
            catch (Exception e)
            {
                result[Constants.ErrorKey] = e.Message;
                callbackManager.OnFacebookResponse(new LoginResult((new ResultContainer(result))));
            }
        }

        public bool IsLoggedIn()
        {
            return fbg.Globals.isLoggedIn();
        }

        public void LogOut()
        {
            fbg.Globals.logout((success) => { Debug.Log("Logged out"); }, (error) => { Debug.LogError(error.Message); });
        }

        public void Tick()
        {
            fbg.Globals.tick();
        }

        public void Deinit()
        {
            this.result = fbg.Globals.deinit();
            Debug.Log("Deinitialized Facebook SDK: " + this.result);
        }

        private List<fbg.LoginScope> ConvertToFbgLoginScope(IEnumerable<string> scope)
        {
            List<fbg.LoginScope> fbgLoginScope = new List<fbg.LoginScope>();
            foreach (string str in scope)
            {
                string result = "";
                string[] subs = str.Split('_');
                foreach (string sub in subs)
                {
                    if (sub.Length == 1)
                    {
                        result += char.ToUpper(sub[0]).ToString();
                    }
                    else
                    {
                        result += (char.ToUpper(sub[0]) + sub.Substring(1)).ToString();
                    }
                }
                if (result != "")
                {
                    fbgLoginScope.Add((fbg.LoginScope)Enum.Parse(typeof(fbg.LoginScope), result));
                }
            }
            return fbgLoginScope;
        }

        public void GetCatalog(string callbackId, CallbackManager callbackManager)
        {
            fbg.Catalog.getCatalog(fbg.PagingType.None, "", 0, (catalogResult) =>
            {
                CatalogResult result = new CatalogResult(WindowsCatalogParser.Parse(catalogResult, callbackId));
                callbackManager.OnFacebookResponse(result);

            }, (error) =>
            {
                PurchaseResult result = new PurchaseResult(WindowsCatalogParser.SetError(error, callbackId));
                callbackManager.OnFacebookResponse(result);
            });
        }

        public void GetPurchases(string callbackId, CallbackManager callbackManager)
        {
            fbg.Purchases.getPurchases(fbg.PagingType.None, "", 0, (purchasesResult) =>
            {
                PurchasesResult result = new PurchasesResult(WindowsPurchaseParser.Parse(purchasesResult, callbackId));
                callbackManager.OnFacebookResponse(result);

            }, (error) =>
            {
                PurchasesResult result = new PurchasesResult(WindowsPurchaseParser.SetError(error, callbackId));
                callbackManager.OnFacebookResponse(result);
            });
        }

        public void Purchase(string newproductID, string newdeveloperPayload, string callbackId, CallbackManager callbackManager)
        {
            fbg.Purchases.purchase(newproductID, newdeveloperPayload, (purchaseResult) =>
            {
                PurchaseResult result = new PurchaseResult(WindowsPurchaseParser.Parse(purchaseResult, callbackId, true));
                callbackManager.OnFacebookResponse(result);
            }, (error) =>
            {
                PurchaseResult result = new PurchaseResult(WindowsPurchaseParser.SetError(error, callbackId));
                callbackManager.OnFacebookResponse(result);
            });
        }

        public void ConsumePurchase(string productToken, string callbackId, CallbackManager callbackManager)
        {
            fbg.Purchases.consume(productToken, (success) =>
            {
                ConsumePurchaseResult result = new ConsumePurchaseResult(new ResultContainer(new Dictionary<string, object>() {
                    {Constants.CallbackIdKey,callbackId }
                }
                ));
                callbackManager.OnFacebookResponse(result);
            }, (error) =>
            {
                ConsumePurchaseResult result = new ConsumePurchaseResult(WindowsPurchaseParser.SetError(error, callbackId));
                callbackManager.OnFacebookResponse(result);
            });
        }

        public void CurrentProfile(string callbackId, CallbackManager callbackManager)
        {
            Dictionary<string, object> result = new Dictionary<string, object>() { { Constants.CallbackIdKey, callbackId } };

            if (IsLoggedIn())
            {
                fbg.Profile.getProfile((windowsProfile) =>
                {
                    if (!String.IsNullOrEmpty(windowsProfile.Raw))
                    {
                        try
                        {
                            Dictionary<string, object> profile = MiniJSON.Json.Deserialize(windowsProfile.Raw) as Dictionary<string, object>;

                            profile.TryGetValue("first_name", out string firstName);
                            profile.TryGetValue("name", out string name);
                            profile.TryGetValue("email", out string email);
                            profile.TryGetValue("picture", out Dictionary<string, object> picture);

                            string imageURL = null;
                            if (picture.TryGetValue("data", out Dictionary<string, object> pictureData))
                            {
                                pictureData.TryGetValue("url", out imageURL);
                            }

                            profile.TryGetValue("link", out string link);

                            result[ProfileResult.ProfileKey] = new Profile(
                                userID,
                                firstName,
                                null,
                                null,
                                name,
                                email,
                                imageURL,
                                link,
                                null,
                                null,
                                null,
                                null,
                                null,
                                null);
                        }
                        catch (Exception e)
                        {
                            result[Constants.ErrorKey] = "ERROR: " + e.Message;
                        }
                    }
                    else
                    {
                        result[Constants.ErrorKey] = "ERROR: No profile data.";
                    }
                    callbackManager.OnFacebookResponse(new ProfileResult((new ResultContainer(result))));

                }, (error) =>
                {
                    string msg = "ERROR: " + error.Message + ",";
                    msg += "InnerErrorCode: " + error.InnerErrorCode.ToString() + ",";
                    msg += "InnerErrorMessage: " + error.InnerErrorMessage + ",";
                    msg += "InnerErrorSubcode: " + error.InnerErrorSubcode.ToString() + ",";
                    msg += "InnerErrorTraceId: " + error.InnerErrorTraceId;

                    result[Constants.ErrorKey] = msg;
                    callbackManager.OnFacebookResponse(new ProfileResult((new ResultContainer(result))));
                });
            }
            else
            {
                result[Constants.ErrorKey] = "ERROR: user must by logged in.";
                callbackManager.OnFacebookResponse(new ProfileResult((new ResultContainer(result))));
            }
        }
        public void LoadInterstitialAd(string placementID, string callbackId, CallbackManager callbackManager)
        {
            fbg.Inappad.loadInterstitialAd(placementID, (response) =>
            {
                Dictionary<string, object> resultDict = new Dictionary<string, object>() {
                    {Constants.CallbackIdKey,callbackId }
                };
                resultDict[Constants.ErrorKey] = response.Error;
                InterstitialAdResult result = new InterstitialAdResult(new ResultContainer(resultDict));
                callbackManager.OnFacebookResponse(result);
            }, (error) =>
            {
                InterstitialAdResult result = new InterstitialAdResult(WindowsParserBase.SetError(error, callbackId));
                callbackManager.OnFacebookResponse(result);
            });
        }

        public void ShowInterstitialAd(string placementID, string callbackId, CallbackManager callbackManager)
        {
            fbg.Inappad.showInterstitialAd(placementID, (response) =>
            {
                Dictionary<string, object> resultDict = new Dictionary<string, object>() {
                    {Constants.CallbackIdKey,callbackId }
                };
                resultDict[Constants.ErrorKey] = response.Error;
                InterstitialAdResult result = new InterstitialAdResult(new ResultContainer(resultDict));
                callbackManager.OnFacebookResponse(result);
            }, (error) =>
            {
                InterstitialAdResult result = new InterstitialAdResult(WindowsParserBase.SetError(error, callbackId));
                callbackManager.OnFacebookResponse(result);
            });
        }

        public void LoadRewardedVideo(string placementID, string callbackId, CallbackManager callbackManager)
        {
            fbg.Inappad.loadRewardedVideo(placementID, (response) =>
            {
                Dictionary<string, object> resultDict = new Dictionary<string, object>() {
                    {Constants.CallbackIdKey,callbackId }
                };
                resultDict[Constants.ErrorKey] = response.Error;
                RewardedVideoResult result = new RewardedVideoResult(new ResultContainer(resultDict));
                callbackManager.OnFacebookResponse(result);
            }, (error) =>
            {
                RewardedVideoResult result = new RewardedVideoResult(WindowsParserBase.SetError(error, callbackId));
                callbackManager.OnFacebookResponse(result);
            });
        }

        public void ShowRewardedVideo(string placementID, string callbackId, CallbackManager callbackManager)
        {
            fbg.Inappad.showRewardedVideo(placementID, (response) =>
            {
                Dictionary<string, object> resultDict = new Dictionary<string, object>() {
                    {Constants.CallbackIdKey,callbackId }
                };
                resultDict[Constants.ErrorKey] = response.Error;
                RewardedVideoResult result = new RewardedVideoResult(new ResultContainer(resultDict));
                callbackManager.OnFacebookResponse(result);
            }, (error) =>
            {
                RewardedVideoResult result = new RewardedVideoResult(WindowsParserBase.SetError(error, callbackId));
                callbackManager.OnFacebookResponse(result);
            });
        }

        public void OpenFriendFinderDialog(string callbackId, CallbackManager callbackManager)
        {
            fbg.Invite.gameInvite(
              (invitation) =>
              {
                  Dictionary<string, object> resultDict = new Dictionary<string, object>() {
                    {Constants.CallbackIdKey,callbackId }
                  };
                  callbackManager.OnFacebookResponse(new FriendFinderInvitationResult(new ResultContainer(resultDict)));
              },
              (error) =>
              {
                  FriendFinderInvitationResult result = new FriendFinderInvitationResult(WindowsParserBase.SetError(error, callbackId));
                  callbackManager.OnFacebookResponse(result);
              }
            );
        }

        public void GetFriendFinderInvitations(string callbackId, CallbackManager callbackManager)
        {
            fbg.ReceivedInvitations.getReceivedInvitations(fbg.PagingType.None, "", 0,
              (fbg.ReceivedInvitations receivedInvitations) =>
              {
                  IList<FriendFinderInviation> invitationsList = new List<FriendFinderInviation>();
                  for (uint i = 0; i < receivedInvitations.Length; ++i)
                  {
                      var item = receivedInvitations[i];
                      invitationsList.Add(new FriendFinderInviation(item.Id, item.ApplicationId, item.ApplicationName, item.FromId, item.FromName, item.ToId, item.ToName, item.Message, item.CreatedTime));
                  }

                  Dictionary<string, object> resultDict = new Dictionary<string, object>() {
                    {Constants.CallbackIdKey,callbackId },
                    {FriendFinderInvitationResult.InvitationsKey,invitationsList }
                  };
                  callbackManager.OnFacebookResponse(new FriendFinderInvitationResult(new ResultContainer(resultDict)));
              },
              (fbg.Error error) =>
              {
                  FriendFinderInvitationResult result = new FriendFinderInvitationResult(WindowsParserBase.SetError(error, callbackId));
                  callbackManager.OnFacebookResponse(result);
              });
        }

        public void DeleteFriendFinderInvitation(string invitationId, string callbackId, CallbackManager callbackManager)
        {
            fbg.ReceivedInvitations.removeInvitation(invitationId,
              (fbg.DeleteInvitation success) =>
              {
                  Dictionary<string, object> resultDict = new Dictionary<string, object>() {
                    {Constants.CallbackIdKey,callbackId },
                  };

                  Dictionary<string, object> deletedInviation = MiniJSON.Json.Deserialize(success.Raw) as Dictionary<string, object>;
                  if (!deletedInviation.TryGetValue(invitationId, out bool deletedOK))
                  {
                      resultDict[Constants.ErrorKey] = "ERROR: wrong deleted inviationID: " + invitationId;

                  }
                  else if (!deletedOK)
                  {
                      resultDict[Constants.ErrorKey] = "ERROR: Fail deleting inviationID: " + invitationId;
                  }

                  callbackManager.OnFacebookResponse(new FriendFinderInvitationResult(new ResultContainer(resultDict)));
              },
              (fbg.Error error) =>
              {
                  FriendFinderInvitationResult result = new FriendFinderInvitationResult(WindowsParserBase.SetError(error, callbackId));
                  callbackManager.OnFacebookResponse(result);
              });
        }

        public void ScheduleAppToUserNotification(string title, string body, Uri media, int timeInterval, string payload, string callbackId, CallbackManager callbackManager)
        {
            fbg.AppToUserNotifications.scheduleAppToUserNotification(title, body, media.ToString(), timeInterval, payload,
                (fbg.ScheduleAppToUserNotificationResult success) =>
                {
                    Dictionary<string, object> resultDict = new Dictionary<string, object>() {
                        {Constants.CallbackIdKey,callbackId },
                    };
                    callbackManager.OnFacebookResponse(new ScheduleAppToUserNotificationResult(new ResultContainer(resultDict)));
                },
                (fbg.Error error) =>
                {
                    ScheduleAppToUserNotificationResult result = new ScheduleAppToUserNotificationResult(WindowsParserBase.SetError(error, callbackId));
                    callbackManager.OnFacebookResponse(result);
                });
        }

        public void PostSessionScore(int score, string callbackId, CallbackManager callbackManager)
        {
            fbg.Tournaments.postSessionScore(score,
                (fbg.PostSessionScoreResult success) =>
                {
                    Dictionary<string, object> resultDict = new Dictionary<string, object>() {
                        {Constants.CallbackIdKey,callbackId },
                        {"Response", success.Raw },
                    };
                    callbackManager.OnFacebookResponse(new SessionScoreResult(new ResultContainer(resultDict)));
                },
                (fbg.Error error) =>
                {
                    SessionScoreResult result = new SessionScoreResult(WindowsParserBase.SetError(error, callbackId));
                    callbackManager.OnFacebookResponse(result);
                });
        }

        public void PostTournamentScore(int score, string callbackId, CallbackManager callbackManager)
        {
            fbg.Tournaments.postTournamentScore(score,
                (fbg.PostTournamentScoreResult success) =>
                {
                    Dictionary<string, object> resultDict = new Dictionary<string, object>() {
                        {Constants.CallbackIdKey,callbackId },
                        {"Response", success.Raw },
                    };
                    callbackManager.OnFacebookResponse(new TournamentScoreResult(new ResultContainer(resultDict)));
                },
                (fbg.Error error) =>
                {
                    TournamentScoreResult result = new TournamentScoreResult(WindowsParserBase.SetError(error, callbackId));
                    callbackManager.OnFacebookResponse(result);
                });
        }

        public void GetTournament(string callbackId, CallbackManager callbackManager)
        {
            fbg.Tournaments.getTournament(
                (fbg.GetTournamentResult tournamentData) =>
                {
                    Dictionary<string, object> resultDict = new Dictionary<string, object>() {
                        {Constants.CallbackIdKey,callbackId },
                    };

                    Dictionary<string, object> response = MiniJSON.Json.Deserialize(tournamentData.Raw) as Dictionary<string, object>;
                    Dictionary<string, object> success = null;
                    if (response.TryGetValue("success", out success))
                    {
                        string tournamentId;
                        if (success.TryGetValue("tournamentId", out tournamentId))
                        {
                            resultDict["tournament_id"] = tournamentId;
                        }
                        string contextId;
                        if (success.TryGetValue("contextId", out contextId))
                        {
                            resultDict["context_id"] = contextId;
                        }
                        int endTime;
                        if (success.TryGetValue("endTime", out endTime))
                        {
                            resultDict["end_time"] = endTime;
                        }
                        string tournamentTitle;
                        if (success.TryGetValue("tournamentTitle", out tournamentTitle))
                        {
                            resultDict["tournament_title"] = tournamentTitle;
                        }
                        IDictionary<string, string> payload;
                        if (success.TryGetValue("payload", out payload))
                        {
                            resultDict["payload"] = payload;
                        }
                    }
                    else
                    {
                        resultDict[Constants.ErrorKey] = "ERROR: Wrong Tournament Data";
                    }

                    callbackManager.OnFacebookResponse(new TournamentResult(new ResultContainer(resultDict)));
                },
                (fbg.Error error) =>
                {
                    TournamentResult result = new TournamentResult(WindowsParserBase.SetError(error, callbackId));
                    callbackManager.OnFacebookResponse(result);
                });
        }

        public void ShareTournament(int score, Dictionary<string, string> data, string callbackId, CallbackManager callbackManager)
        {
            fbg.Tournaments.shareTournament(score,
                MiniJSON.Json.Serialize(data),
                (fbg.ShareTournamentResult success) =>
                {
                    Dictionary<string, object> resultDict = new Dictionary<string, object>() {
                        {Constants.CallbackIdKey,callbackId },
                        {"Response", success.Raw },
                    };
                    callbackManager.OnFacebookResponse(new TournamentScoreResult(new ResultContainer(resultDict)));
                },
                (fbg.Error error) =>
                {
                    TournamentScoreResult result = new TournamentScoreResult(WindowsParserBase.SetError(error, callbackId));
                    callbackManager.OnFacebookResponse(result);
                });
        }

        public void CreateTournament(int initialScore, string title, string imageBase64DataUrl, string sortOrder, string scoreFormat, Dictionary<string, string> data, string callbackId, CallbackManager callbackManager)
        {
            fbg.Tournaments.createTournament(
                initialScore,
                title,
                imageBase64DataUrl,
                sortOrder,
                scoreFormat,
                MiniJSON.Json.Serialize(data),
                (fbg.CreateTournamentResult tournamentData) =>
                {
                    Dictionary<string, object> resultDict = new Dictionary<string, object>() {
                        {Constants.CallbackIdKey,callbackId },
                    };

                    Dictionary<string, object> response = MiniJSON.Json.Deserialize(tournamentData.Raw) as Dictionary<string, object>;
                    Dictionary<string, object> success = null;
                    if (response.TryGetValue("success", out success))
                    {
                        string tournamentId;
                        if (success.TryGetValue("tournamentId", out tournamentId))
                        {
                            resultDict["tournament_id"] = tournamentId;
                        }
                        string contextId;
                        if (success.TryGetValue("contextId", out contextId))
                        {
                            resultDict["context_id"] = contextId;
                        }
                        int endTime;
                        if (success.TryGetValue("endTime", out endTime))
                        {
                            resultDict["end_time"] = endTime;
                        }
                        string tournamentTitle;
                        if (success.TryGetValue("tournamentTitle", out tournamentTitle))
                        {
                            resultDict["tournament_title"] = tournamentTitle;
                        }
                        IDictionary<string, string> payload;
                        if (success.TryGetValue("payload", out payload))
                        {
                            resultDict["payload"] = payload;
                        }
                    }
                    else
                    {
                        resultDict[Constants.ErrorKey] = "ERROR: Wrong Tournament Data";
                    }
                    callbackManager.OnFacebookResponse(new TournamentResult(new ResultContainer(resultDict)));
                },
                (fbg.Error error) =>
                {
                    TournamentResult result = new TournamentResult(WindowsParserBase.SetError(error, callbackId));
                    callbackManager.OnFacebookResponse(result);
                });
        }

        public void UploadImageToMediaLibrary(string caption, Uri imageUri, bool shouldLaunchMediaDialog, string travelId, string callbackId, CallbackManager callbackManager)
        {
            fbg.Share.uploadImageToMediaLibrary(
                caption,
                imageUri.LocalPath,
                shouldLaunchMediaDialog,
                travelId,
                (fbg.UploadImageToMediaLibraryResult success) =>
                {
                    Dictionary<string, object> resultDict = new Dictionary<string, object>() {
                        {Constants.CallbackIdKey,callbackId },
                        {"id", success.Id },
                    };
                    callbackManager.OnFacebookResponse(new MediaUploadResult(new ResultContainer(resultDict)));
                }, (fbg.Error error) =>
                {
                    MediaUploadResult result = new MediaUploadResult(WindowsParserBase.SetError(error, callbackId));
                    callbackManager.OnFacebookResponse(result);
                });
        }

        public void UploadVideoToMediaLibrary(string caption, Uri videoUri, bool shouldLaunchMediaDialog, string travelId, string callbackId, CallbackManager callbackManager)
        {
            fbg.Share.uploadVideoToMediaLibrary(
               caption,
               videoUri.LocalPath,
               shouldLaunchMediaDialog,
               travelId,
               (fbg.UploadVideoToMediaLibraryResult success) =>
               {
                   Dictionary<string, object> resultDict = new Dictionary<string, object>() {
                        {Constants.CallbackIdKey,callbackId },
                        {"video_id", success.VideoId },
                    };
                   callbackManager.OnFacebookResponse(new MediaUploadResult(new ResultContainer(resultDict)));
               }, (fbg.Error error) =>
               {
                   MediaUploadResult result = new MediaUploadResult(WindowsParserBase.SetError(error, callbackId));
                   callbackManager.OnFacebookResponse(result);
               });
        }

        public void SetVirtualGamepadLayout(string layout, string callbackId, CallbackManager callbackManager)
        {
            fbg.Virtualgamepad.setVirtualGamepadLayout(layout,
                (fbg.VirtualGamepadLayoutResult success) =>
                {
                    Dictionary<string, object> resultDict = new Dictionary<string, object>() {
                        { Constants.CallbackIdKey, callbackId },
                        { "success", success },
                    };
                    callbackManager.OnFacebookResponse(new VirtualGamepadLayoutResult(new ResultContainer(resultDict)));
                }, (fbg.Error error) =>
                {
                    VirtualGamepadLayoutResult result = new VirtualGamepadLayoutResult(WindowsParserBase.SetError(error, callbackId));
                    callbackManager.OnFacebookResponse(result);
                });
        }

        public void GetUserLocale(string callbackId, CallbackManager callbackManager)
        {
            Dictionary<string, object> result = new Dictionary<string, object>() { { Constants.CallbackIdKey, callbackId } };

            if (IsLoggedIn())
            {
                fbg.Profile.getProfile((windowsProfile) =>
                {
                    result.Add("locale", windowsProfile.Locale);
                    callbackManager.OnFacebookResponse(new LocaleResult((new ResultContainer(result))));

                }, (error) =>
                {
                    string msg = "ERROR: " + error.Message + ",";
                    msg += "InnerErrorCode: " + error.InnerErrorCode.ToString() + ",";
                    msg += "InnerErrorMessage: " + error.InnerErrorMessage + ",";
                    msg += "InnerErrorSubcode: " + error.InnerErrorSubcode.ToString() + ",";
                    msg += "InnerErrorTraceId: " + error.InnerErrorTraceId;

                    result[Constants.ErrorKey] = msg;
                    callbackManager.OnFacebookResponse(new LocaleResult(new ResultContainer(result)));
                });
            }
            else
            {
                result[Constants.ErrorKey] = "ERROR: user must be logged in.";
                callbackManager.OnFacebookResponse(new LocaleResult(new ResultContainer(result)));
            }
        }

        public void SetSoftKeyboardOpen(bool open, string callbackId, CallbackManager callbackManager)
        {
            fbg.Softkeyboard.setSoftKeyboardOpen(open,
                (fbg.SoftkeyboardAction success) =>
                {
                    Dictionary<string, object> resultDict = new Dictionary<string, object>() {
                        { Constants.CallbackIdKey, callbackId },
                        { "success", success },
                    };
                    callbackManager.OnFacebookResponse(new SoftKeyboardOpenResult(new ResultContainer(resultDict)));
                }, (fbg.Error error) =>
                {
                    SoftKeyboardOpenResult result = new SoftKeyboardOpenResult(WindowsParserBase.SetError(error, callbackId));
                    callbackManager.OnFacebookResponse(result);
                });
        }

        public void CreateReferral(string payload, string callbackId, CallbackManager callbackManager)
        {
            fbg.Referrals.createReferral(payload,
                (fbg.CreateReferralResult success) =>
                {
                    Dictionary<string, object> resultDict = new Dictionary<string, object>() {
                        { Constants.CallbackIdKey, callbackId },
                        { "raw", success.Raw },
                        { "referral_link", success.ReferralLink },
                    };
                    callbackManager.OnFacebookResponse(new ReferralsCreateResult(new ResultContainer(resultDict)));
                }, (fbg.Error error) =>
                {
                    ReferralsCreateResult result = new ReferralsCreateResult(WindowsParserBase.SetError(error, callbackId));
                    callbackManager.OnFacebookResponse(result);
                });
        }

        public void GetDataReferral(string callbackId, CallbackManager callbackManager)
        {
            fbg.Referrals.getDataReferral(
                (fbg.GetDataReferralResult success) =>
                {
                    Dictionary<string, object> resultDict = new Dictionary<string, object>() {
                        { Constants.CallbackIdKey, callbackId },
                        { "raw", success.Raw },
                        { "payload", success.Payload },
                    };
                    callbackManager.OnFacebookResponse(new ReferralsGetDataResult(new ResultContainer(resultDict)));
                }, (fbg.Error error) =>
                {
                    ReferralsGetDataResult result = new ReferralsGetDataResult(WindowsParserBase.SetError(error, callbackId));
                    callbackManager.OnFacebookResponse(result);
                });
        }
    }
}
