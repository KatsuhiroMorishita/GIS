/******************************************************
 * Point.cs
 * KMLの点を表すクラス
 * 
 * [更新履歴]
 *      2012/6/2    開発開始
 *      2012/7/12   コメント内容を若干変更
 *                  バインドされたオブジェクトをさらにバインドした場合でもKMLコードを出力できるように変更した。
 *                  また、スタイルにCommonを用いた場合でもスタイルIDを上書きしてしまわないように変更を加えた。
 * ****************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GNSS;

namespace GIS.KML.Point
{
    /// <summary>
    /// KML形式の点を表すクラス
    /// <para>点をGoogle Earth上にプロットさせたい場合に便利です。</para>
    /// </summary>
    public class Point: KML.Base<Point>
    {
        /* プロパティ *********************************/
        /// <summary>
        /// スケール
        /// <para>サイズを調整します。</para>
        /// </summary>
        public double scale
        {
            get;
            set;
        }
        /// <summary>
        /// アイコン
        /// <para>表示するアイコンを変更可能です。</para>
        /// </summary>
        public Icon icon
        {
            get;
            set;
        }
        /// <summary>
        /// 位置座標
        /// </summary>
        public Blh Pos
        {
            get;
            set;
        }
        /* メソッド *********************************/
        /// <summary>
        /// スタイルコードを生成する
        /// <para>スタイルコード生成後はPlacemarkとの整合性を保つためにStyleIDを変更しないでください。</para>
        /// </summary>
        /// <param name="mode">スタイルモード<para>ユニークなスタイルとするかどうかを決定します。</para></param>
        /// <returns>スタイルコード</returns>
        protected override string GetStyleCode(StyleMode mode = StyleMode.Common)
        {
            StringBuilder sb = new StringBuilder(300);                      // 予め大きなメモリ容量を確保しておく
            sb.Append(@"    <Style id=""").Append(this.StyleID).Append(@""">").Append(System.Environment.NewLine);
            sb.Append(@"      <IconStyle>").Append(System.Environment.NewLine);
            sb.Append(@"        <color>").Append(this.ConvertColorToKmlColor(this.color)).Append("</color>").Append(System.Environment.NewLine);
            sb.Append(@"        <scale>").Append(this.scale.ToString("0.00")).Append("</scale>").Append(System.Environment.NewLine);
            sb.Append(@"        <Icon>").Append(System.Environment.NewLine);
            sb.Append(@"          <href>").Append("http://maps.google.com/mapfiles/kml/shapes/").Append(this.icon.ToString()).Append(".png</href>").Append(System.Environment.NewLine);
            sb.Append(@"        </Icon>").Append(System.Environment.NewLine);
            sb.Append(@"      </IconStyle>").Append(System.Environment.NewLine);
            sb.Append(@"    </Style>").Append(System.Environment.NewLine);
            if (mode == StyleMode.Unique) 
                foreach (var mem in this.buffer)
                    sb.Append(mem.GetStyleCode(mode));
            return sb.ToString();
        }
        /// <summary>
        /// Placemarkコードを返す
        /// </summary>
        /// <param name="mode">スタイルモード<para>ユニークなスタイルとするかどうかを決定します。</para></param>
        /// <param name="styleID">スタイルID<para>スタイルモードがCommonである場合に適用するIDです。</para></param>
        /// <returns>Placemarkコード</returns>
        protected override string GetKmlCoreCode(StyleMode mode, string styleID)
        {
            StringBuilder sb = new StringBuilder(200);                      // 予め大きなメモリ容量を確保しておく
            sb.Append(@"    <Placemark>").Append(System.Environment.NewLine);
            if (this.Time != null) sb.Append(this.Time.GetKmlTime());
            sb.Append(@"      <styleUrl>#");
            if (mode == StyleMode.Unique)
                sb.Append(this.StyleID);
            else
                sb.Append(styleID);
            sb.Append("</styleUrl>").Append(System.Environment.NewLine);
            if (this.name != "") sb.Append(@"      <name>").Append(this.name).Append("</name>").Append(System.Environment.NewLine);
            if (this.description != "") sb.Append(@"      <description>").Append(this.KmledDescription).Append("</description>").Append(System.Environment.NewLine);
            sb.Append(@"      <Point>").Append(System.Environment.NewLine);
            sb.Append(@"        <coordinates>").Append(this.Pos.ToString()).Append("</coordinates>").Append(System.Environment.NewLine);
            sb.Append(@"      </Point>").Append(System.Environment.NewLine);
            sb.Append(@"    </Placemark>").Append(System.Environment.NewLine);
            foreach (var mem in this.buffer)
                sb.Append(mem.GetKmlCoreCode(mode, styleID));
            return sb.ToString();
        }
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public Point(Icon icon = Icon.shaded_dot)
            :base()
        {
            this.scale = 1.2;
            this.icon = icon;
            this.Pos = new GNSS.Blh();
        }
        /// <summary>
        /// 座標の初期化付コンストラクタ
        /// </summary>
        /// <param name="pos">座標</param>
        /// <param name="icon">アイコン</param>
        public Point(Blh pos, Icon icon = Icon.shaded_dot)
            : this(icon)
        {
            this.Pos = pos;
        }
        /// <summary>
        /// 座標と時刻の初期化付コンストラクタ
        /// </summary>
        /// <param name="pos">座標</param>
        /// <param name="time">時刻</param>
        /// <param name="icon">アイコン</param>
        public Point(Blh pos, DateTime time, Icon icon = Icon.shaded_dot)
            : this(icon)
        {
            this.Pos = pos;
        }
    }
}
