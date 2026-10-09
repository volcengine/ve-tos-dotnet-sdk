/*
 * Copyright (2023) Volcengine
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using TOS.Common;

namespace TOS.Model
{
    public class RestoreInfo
    {
        public RestoreStatus RestoreStatus { internal set; get; }

        public RestoreParam RestoreParam { internal set; get; }
    }

    public class RestoreStatus
    {
        public bool OngoingRequest { internal set; get; }

        public DateTime? ExpiryDate { internal set; get; }
    }

    public class RestoreParam
    {
        public DateTime? RequestDate { internal set; get; }

        public int? ExpiryDays { internal set; get; }

        public TierType? Tier { internal set; get; }
    }

    internal static class RestoreHeaderParser
    {
        // Quoted HTTP dates contain commas; splitting the header on commas loses the date.
        private static readonly Regex Parameters = new Regex(
            @"(?:^|,)\s*(?<key>[\w-]+)\s*=\s*(?:""(?<value>[^""]*)""|(?<value>[^,\s""]+))\s*(?=,|$)");

        internal static RestoreInfo Parse(IDictionary<string, string> header)
        {
            string restore;
            header.TryGetValue(Constants.HeaderRestore, out restore);
            if (string.IsNullOrEmpty(restore))
            {
                return null;
            }

            RestoreStatus status = new RestoreStatus();
            foreach (Match match in Parameters.Matches(restore))
            {
                string key = match.Groups["key"].Value;
                string value = match.Groups["value"].Value;
                if (key == "ongoing-request")
                {
                    bool ongoingRequest;
                    status.OngoingRequest = bool.TryParse(value, out ongoingRequest) && ongoingRequest;
                }
                else if (key == "expiry-date")
                {
                    status.ExpiryDate = ParseDate(value);
                }
            }

            RestoreInfo result = new RestoreInfo { RestoreStatus = status };
            string requestDate;
            string expiryDays;
            string tier;
            header.TryGetValue(Constants.HeaderRestoreRequestDate, out requestDate);
            header.TryGetValue(Constants.HeaderRestoreExpiryDays, out expiryDays);
            header.TryGetValue(Constants.HeaderRestoreTier, out tier);
            if (!string.IsNullOrEmpty(requestDate) || !string.IsNullOrEmpty(expiryDays) ||
                !string.IsNullOrEmpty(tier))
            {
                int days;
                result.RestoreParam = new RestoreParam
                {
                    RequestDate = ParseDate(requestDate),
                    ExpiryDays = int.TryParse(expiryDays, NumberStyles.Integer,
                        Constants.DefaultCultureInfo, out days) && days >= 0 ? (int?)days : null,
                    Tier = Enums.ParseEnum<TierType>(tier) as TierType?
                };
            }

            return result;
        }

        private static DateTime? ParseDate(string value)
        {
            DateTime date;
            return DateTime.TryParseExact(value, Constants.Rfc1123DateFormat,
                Constants.DefaultCultureInfo, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out date) ? (DateTime?)date : null;
        }
    }
}
