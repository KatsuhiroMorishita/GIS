using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GIS.KML.Time
{
    /// <summary>
    /// ある瞬間の時間を表す構造体
    /// </summary>
    public struct TimeStamp: ITime
    {
        /// <summary>
        /// 時刻
        /// </summary>
        public DateTime Epoch
        {
            get;
            private set;
        }
        /// <summary>
        /// 日付をKML用に加工する
        /// </summary>
        /// <returns>日付</returns>
        private string GetDate()
        {
            return this.Epoch.Year.ToString("00") + "-" + this.Epoch.Month.ToString("00") + "-" + this.Epoch.Day.ToString("00") + "T";
        }
        /// <summary>
        /// 時刻をKML用に加工する
        /// </summary>
        /// <returns>時刻</returns>
        private string GetClock()
        {
            return this.Epoch.Hour.ToString("00") + ":" + this.Epoch.Minute.ToString("00") + ":" + this.Epoch.Second.ToString("00") + "Z";
        }
        /// <summary>
        /// 文字列化します
        /// </summary>
        /// <returns>文字列化したタイムスタンプ</returns>
        public string GetKmlTime()
        {
            StringBuilder sb = new StringBuilder(200);                      // 予め大きなメモリ容量を確保しておく
            sb.Append(@"      <TimeStamp>").Append(System.Environment.NewLine);
            sb.Append(@"        <when>").Append(this.GetDate()).Append(this.GetClock()).Append("</when>").Append(System.Environment.NewLine);
            sb.Append(@"      </TimeStamp>").Append(System.Environment.NewLine);
            return sb.ToString();
        }
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="epoch">タイムスタンプの時刻</param>
        public TimeStamp(DateTime epoch)
            :this()
        {
            this.Epoch = epoch;
        }
    }
}
