using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GIS.KML.Time
{
    /// <summary>
    /// 期間を表す構造体
    /// </summary>
    public struct TimeSpan: ITime
    {
        /// <summary>
        /// 時刻
        /// </summary>
        public DateTime Start
        {
            get;
            private set;
        }
        /// <summary>
        /// 時刻
        /// </summary>
        public DateTime End
        {
            get;
            private set;
        }
        /// <summary>
        /// 文字列化します
        /// </summary>
        /// <returns>文字列化したタイムスパン</returns>
        public string GetKmlTime()
        {
            StringBuilder sb = new StringBuilder(200);                      // 予め大きなメモリ容量を確保しておく
            sb.Append(@"      <TimeSpan>").Append(System.Environment.NewLine);
            sb.Append(@"        <begin>").Append(this.Start.ToString()).Append("</begin>").Append(System.Environment.NewLine);
            sb.Append(@"        <end>").Append(this.Start.ToString()).Append("</end>").Append(System.Environment.NewLine);
            sb.Append(@"      </TimeSpan>").Append(System.Environment.NewLine);
            return sb.ToString();
        }
        /// <summary>
        /// コンストラクタ
        /// <para>引数の時系列チェックは行っていませんのでご注意ください。</para>
        /// </summary>
        /// <param name="start">開始時刻</param>
        /// <param name="end">終了時刻</param>
        public TimeSpan(DateTime start, DateTime end)
            :this()
        {
            this.Start = start;
            this.End = end;
        }
    }
}
