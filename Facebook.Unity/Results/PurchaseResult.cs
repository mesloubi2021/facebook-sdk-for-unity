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

namespace Facebook.Unity
{
    using System.Collections.Generic;
    using System.Text;

    internal class PurchaseResult : ResultBase, IPurchaseResult
    {
        public PurchaseResult(ResultContainer resultContainer) : base(resultContainer)
        {
            if (this.ResultDictionary != null && this.ResultDictionary.ContainsKey("success"))
            {
                this.Purchase = Utilities.ParsePurchaseFromResult(this.ResultDictionary);
            }
            else if (this.ResultDictionary != null && this.ResultDictionary.ContainsKey("purchase"))
            {
                this.ResultDictionary.TryGetValue("purchase", out object product);
                this.Purchase = (Purchase)product;
            }

            if (this.ErrorDictionary != null && this.ErrorDictionary["errorType"] == "USER_INPUT")
            {
                this.Cancelled = true;
            }
        }

        public Purchase Purchase { get; private set; }

        public override string ToString()
        {
            return Utilities.FormatToString(
                null,
                this.GetType().Name,
                new Dictionary<string, string>()
                {
                    { "Purchase", Purchase.ToString() },
                });
        }
    }
}
