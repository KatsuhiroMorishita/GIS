/****************************************************************************
 * Polygon.cs
 * KMLのポリゴンを生成するクラス
 * 
 * [目的]
 *  ポリゴンの生成を目的としています。
 * 
 * [history]
 *      2012/5/24   開発開始
 *                  KMLの座標系はどうすんだ？
 *      2012/7/12   バインドされたオブジェクトをさらにバインドした場合でもKMLコードを出力できるように変更した。
 *                  また、スタイルにCommonを用いた場合でもスタイルIDを上書きしてしまわないように変更を加えた。
 * **************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using GNSS;
using GIS.KML;

namespace GIS.KML.Polygon
{
    /// <summary>
    /// Polygonクラス
    /// <para>プロパティ名等はKMLに合わせています。</para>
    /// </summary>
    public class Polygon : Base<Polygon>
    {
        /* メンバ変数 *********************************/
        /// <summary>
        /// 外側の点情報
        /// </summary>
        private List<Blh> outerBuff;
        /// <summary>
        /// 内側の点情報
        /// </summary>
        private List<Blh> innerBUff;
        /* プロパティ *********************************/
        /* メソッド *********************************/
        /// <summary>
        /// 内側に点を追加する
        /// </summary>
        /// <param name="pos">位置座標</param>
        public void AddToInner(Blh pos)
        {
            this.innerBUff.Add(pos.ToDegree());
        }
        /// <summary>
        /// 内側に点を追加する
        /// </summary>
        /// <param name="pos">位置座標</param>
        public void AddToInner(Blh[] pos)
        {
            foreach (Blh position in pos)
            {
                this.innerBUff.Add(position.ToDegree());
            }
            return;
        }
        /// <summary>
        /// 外側へ点を追加する
        /// </summary>
        /// <param name="pos">位置座標</param>
        public void AddToOuter(Blh pos)
        {
            this.outerBuff.Add(pos.ToDegree());
        }
        /// <summary>
        /// 外側へ点を追加する
        /// </summary>
        /// <param name="pos">位置座標</param>
        public void AddToOuter(Blh[] pos)
        {
            foreach (Blh position in pos)
            {
                this.outerBuff.Add(position.ToDegree());
            }
            return;
        }
        /// <summary>
        /// 本インスタンスのスタイルコードを返す
        /// </summary>
        /// <param name="mode">スタイルモード<para>ユニークなスタイルとするかどうかを決定します。</para></param>
        /// <returns>スタイルコード</returns>
        protected override string GetStyleCode(StyleMode mode = StyleMode.Common)
        {
            StringBuilder sb = new StringBuilder(300);                      // 予め大きなメモリ容量を確保しておく
            sb.Append(@"    <Style id=""").Append(this.StyleID).Append(@""">").Append(System.Environment.NewLine);
            sb.Append(@"      <LineStyle>").Append(System.Environment.NewLine);
            sb.Append(@"        <width>1.5</width>").Append(System.Environment.NewLine);
            sb.Append(@"      </LineStyle>").Append(System.Environment.NewLine);
            sb.Append(@"      <PolyStyle>").Append(System.Environment.NewLine);
            sb.Append(@"        <color>").Append(this.ConvertColorToKmlColor(this.color)).Append("</color>").Append(System.Environment.NewLine);
            sb.Append(@"      </PolyStyle>").Append(System.Environment.NewLine);
            sb.Append(@"    </Style>").Append(System.Environment.NewLine);
            if (mode == StyleMode.Unique)
                foreach (var mem in this.buffer)
                    sb.Append(mem.GetStyleCode(mode));
            return sb.ToString();
        }
        /// <summary>
        /// 本インスタンスのポリゴンのコア部分のみのコードを返す
        /// </summary>
        /// <param name="mode">スタイルモード<para>ユニークなスタイルとするかどうかを決定します。</para></param>
        /// <param name="styleID">スタイルID<para>スタイルモードがCommonである場合に適用するIDです。</para></param>
        /// <returns>ポリゴンのコアコード</returns>
        protected override string GetKmlCoreCode(StyleMode mode, string styleID)
        {
            StringBuilder sb = new StringBuilder(1000);                      // 予め大きなメモリ容量を確保しておく
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
            sb.Append(@"       <Polygon>
        <extrude>1</extrude>
        <altitudeMode>clampToGround</altitudeMode>
        <outerBoundaryIs>
          <LinearRing>
            <coordinates>").Append(System.Environment.NewLine); ;
            foreach (Blh pos in this.outerBuff)
            {
                sb.Append("              ").Append(pos.ToString()).Append(System.Environment.NewLine);
            }
            sb.Append(@"            </coordinates>
          </LinearRing>
        </outerBoundaryIs>").Append(System.Environment.NewLine); 
            sb.Append(@"      <innerBoundaryIs>
        <LinearRing>
          <coordinates>").Append(System.Environment.NewLine); 
            foreach (Blh pos in this.innerBUff)
            {
                sb.Append("              ").Append(pos.ToString()).Append(System.Environment.NewLine);
            }
            sb.Append(@"          </coordinates>
        </LinearRing>
      </innerBoundaryIs>").Append(System.Environment.NewLine);
            sb.Append(@"      </Polygon>").Append(System.Environment.NewLine);
            sb.Append(@"    </Placemark>").Append(System.Environment.NewLine);
            foreach (var mem in this.buffer)
                sb.Append(mem.GetKmlCoreCode(mode, styleID));
            return sb.ToString();
        }
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public Polygon()
            :base()
        {
            this.name = "";
            this.color = Color.MediumBlue;            // 半透明青
            this.outerBuff = new List<Blh>();
            this.innerBUff = new List<Blh>();
        }
    }
}
