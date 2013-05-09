/******************************************************
 * Base.cs
 * KMLのための基本クラス
 * 
 * [更新履歴]
 *      2012/6/2    開発開始
 *      2012/6/6    ほぼ基本機能の実装を終えた。
 *                  時間に関する実装をどのように行えばいいのか、考慮中・・・。
 *      2012/6/9    GetKmlHeaderCode()とGetKmlEndCode()のアクセスレベルをprotectedからprivateへ変更した。
 *      2012/7/12	バインドされているオブジェクト同士の結合でも支障がないように、GetKmlCode()を書き換えた。
 *                  ConvertColorToKmlColor()をprotectedへ変更した。
 *                  また、一部のコメントを見直した。
 *      2012/8/26   AddDescription()を高速化
 * ****************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace GIS.KML
{
    /// <summary>
    /// 各KML要素の共通プロパティ等をここで宣言しておく
    /// </summary>
    public abstract class Base<T> where T: Base<T>
    {
        /* メンバ変数 *********************************/
        /// <summary>
        /// 同じ型の入れ子バッファ
        /// <para>グルーピングを行う際に利用してください。</para>
        /// </summary>
        protected List<T> buffer;
        /* プロパティ *********************************/
        /// <summary>
        /// 名前
        /// </summary>
        public string name
        {
            get;
            set;
        }
        /// <summary>
        /// 説明文
        /// </summary>
        public string description
        {
            get;
            set;
        }
        /// <summary>
        /// 色
        /// </summary>
        public Color color
        {
            get;
            set;
        }
        /// <summary>
        /// スタイルコードに使用可能なオブジェクト独自のIDを返す
        /// <para>ハッシュコードを利用したIDを返します。</para>
        /// </summary>
        protected string StyleOriginalID
        {
            get
            {
                return this.GetHashCode().ToString();
            }
        }
        /// <summary>
        /// スタイルコードに使用するID
        /// </summary>
        public string StyleID
        {
            get;
            private set;
        }
        /// <summary>
        /// KML用にタグを付けた説明文
        /// </summary>
        public string KmledDescription
        {
            get 
            {
                return "<![CDATA[" + this.description + "]]>";
            }
        }
        /// <summary>
        /// 時刻情報
        /// </summary>
        public Time.ITime Time
        {
            get;
            set;
        }
        /* メソッド *********************************/
        /// <summary>
        /// 色構造体をKMLのフォーマットに合わせて文字列化して返す
        /// </summary>
        /// <param name="color">色</param>
        /// <returns>色情報</returns>
        protected string ConvertColorToKmlColor(Color color)
        {
            StringBuilder sb = new StringBuilder(8);
            if (color.A < 16) 
                sb.Append("0").Append(Convert.ToString(color.A, 16));
            else
                sb.Append(Convert.ToString(color.A, 16));
            if (color.B < 16)
                sb.Append("0").Append(Convert.ToString(color.B, 16));
            else
                sb.Append(Convert.ToString(color.B, 16));
            if (color.G < 16)
                sb.Append("0").Append(Convert.ToString(color.G, 16));
            else
                sb.Append(Convert.ToString(color.G, 16));
            if (color.R < 16)
                sb.Append("0").Append(Convert.ToString(color.R, 16));
            else
                sb.Append(Convert.ToString(color.R, 16));
            return sb.ToString();
        }
        /// <summary>
        /// 同じオブジェクトを内部へ格納する
        /// <para>KMLコードのバインドを想定した機能です。</para>
        /// </summary>
        /// <param name="add">格納したいオブジェクト</param>
        public void Add(T add)
        {
            this.buffer.Add(add);
            return;
        }
        /// <summary>
        /// 説明文を追加する
        /// </summary>
        /// <param name="text">追加するテキスト</param>
        public void AddDescription(string text)
        {
            StringBuilder sb = new StringBuilder(300);
            sb.Append(this.description).Append(System.Environment.NewLine).Append(text);
            this.description = sb.ToString();
            return;
        }
        /// <summary>
        /// KMLのヘッダ部分を返す
        /// </summary>
        /// <returns>KMLのヘッダ部分</returns>
        private string GetKmlHeaderCode()
        {
            StringBuilder sb = new StringBuilder(200);                      // 予め大きなメモリ容量を確保しておく
            sb.Append(@"<?xml version=""1.0"" encoding=""UTF-8""?>").Append(System.Environment.NewLine);
            sb.Append(@"<kml xmlns=""http://www.opengis.net/kml/2.2"">").Append(System.Environment.NewLine);
            sb.Append(@"  <Document>").Append(System.Environment.NewLine);
            return sb.ToString();
        }
        /// <summary>
        /// KMLの終端部分を返す
        /// </summary>
        /// <returns>KMLの終端部分</returns>
        private string GetKmlEndCode()
        {
            StringBuilder sb = new StringBuilder(50);                       // 予め大きなメモリ容量を確保しておく
            sb.Append(@"  </Document>").Append(System.Environment.NewLine);
            sb.Append(@"</kml>").Append(System.Environment.NewLine);
            return sb.ToString();
        }
        /// <summary>
        /// スタイルコードを返す
        /// <para>継承したクラスがオーバーライドしてください。</para>
        /// <para>[2012/6/6]スタイル自体も構造体にした方がよさそうですが、今は放置しておきます。</para>
        /// </summary>
        /// <param name="mode">スタイルモード<para>ユニークなスタイルとするかどうかを決定します。</para></param>
        /// <returns>スタイルコード</returns>
        protected virtual string GetStyleCode(StyleMode mode)
        {
            return "";
        }
        /// <summary>
        /// KMLとしての本オブジェクトのコアコードを返す
        /// <para>継承したクラスがオーバーライドしてください。</para>
        /// <para>スタイルコードは含まれません。</para>
        /// </summary>
        /// <param name="mode">スタイルモード<para>ユニークなスタイルとするかどうかを決定します。</para></param>
        /// <param name="styleID">スタイルID<para>スタイルモードがCommonである場合に適用するIDです。</para></param>
        /// <returns>本オブジェクトのコアコード</returns>
        protected virtual string GetKmlCoreCode(StyleMode mode, string styleID)
        {
            return "";
        }
        /// <summary>
        /// KMLコードを返す
        /// <para>そのままファイルへ保存可能なテキストコードを返します。</para>
        /// <para>保存の際は文字コードにUTF8を選択してください。</para>
        /// </summary>
        /// <param name="mode">スタイルモード<para>ユニークなスタイルとするかどうかを決定します。</para></param>
        /// <returns>KML完全コード</returns>
        public string GetKmlCode(StyleMode mode = StyleMode.Common)
        {
            StringBuilder sb = new StringBuilder(1500);                      // 予め大きなメモリ容量を確保しておく
            sb.Append(this.GetKmlHeaderCode());
            sb.Append(this.GetStyleCode(mode));
            sb.Append(this.GetKmlCoreCode(mode, this.StyleID));
            sb.Append(this.GetKmlEndCode());
            return sb.ToString();
        }
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public Base()
        {
            this.buffer = new List<T>();
            this.name = "";
            this.description = "";
            this.color = Color.MediumBlue;
            this.StyleID = this.StyleOriginalID;
            this.Time = null;
        }
    }
}
