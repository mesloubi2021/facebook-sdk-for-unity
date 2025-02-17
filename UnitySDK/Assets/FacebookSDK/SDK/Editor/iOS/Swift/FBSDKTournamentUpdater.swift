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

import FBSDKCoreKit
import FBSDKGamingServicesKit
import Foundation

/// An internal class for fetching tournament objects.
@objcMembers
public final class FBSDKTournamentUpdater: NSObject {

    public override init() {
        super.init()
    }

  /**
   Updates the given tournament with the given score
   - Parameter tournamentID: The ID of the tournament you want to update
   - Parameter score: The new score to update within the tournament
   - Parameter completionHandler: The caller's completion handler to invoke once the graph request is complete. Completes with `true` if successful.
   */
  public func update(
    tournamentID: String,
    score: Int,
    completionHandler: @escaping (Bool, Error?) -> Void
  ) {
    guard !tournamentID.isEmpty else {
      return completionHandler(false, TournamentUpdaterError.invalidTournamentID)
    }
    TournamentUpdater()
      .update(tournamentID: tournamentID, score: score) { result in
        switch result {
        case let .success(success):
          completionHandler(success, nil)
        case let .failure(error):
          completionHandler(false, error)
        }
      }
  }

  /**
   Updates the given tournament with the given score
   - Parameter tournament: The tournament you want to update
   - Parameter score: The new score to update within the tournament
   - Parameter completionHandler: The caller's completion handler to invoke once the graph request is complete. Completes with `true` if successful.
   */

  public func update(
    tournament: FBSDKTournament,
    score: Int,
    completionHandler: @escaping (Bool, Error?) -> Void
  ) {
    update(tournamentID: tournament.identifier, score: score, completionHandler: completionHandler)
  }
}
