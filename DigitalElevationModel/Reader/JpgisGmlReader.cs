/****************************************************************************
 * JPGmlReader.cs
 * JPGIS(GML)形式のDEMを読み出すクラス
 * 
 * 日本のDEMだと未定義の値は-9999.0となっているが、これは後々の取り扱いが面倒であるので、float.NaNと置換する。
 * 
 * [課題]
 *      1) 
 * 
 * [history]
 *      2012/5/22   開発開始
 *      2012/5/24   基本機能の全実装を終えた。
 *      2012/5/25   正規表現で考慮漏れがあった分をカバー。
 *      2012/5/28   -9999.0をfloat.NaNへ置換するように変更した。
 *      2012/8/11   コメントを一部見直し。
 * **************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;   // for Regex
using GIS.MapBase;
using GNSS;
using GNSS.Field;

namespace GIS.DigitalElevationModel.Reader
{
    /// <summary>
    /// JPGIS(GML)形式のDEMを読み出す静的クラス
    /// </summary>
    public class JpgisGmlReader
    {
        /* メンバ変数 *****************************************************/
        /// <summary>
        /// IDにヒットする正規表現オブジェクト
        /// </summary>
        private Regex meshID;
        /// <summary>
        /// 下側のコーナー座標にヒットする正規表現オブジェクト
        /// </summary>
        private Regex lowerCorner;
        /// <summary>
        /// 上側のコーナー座標にヒットする正規表現オブジェクト
        /// </summary>
        private Regex upperCorner;
        /// <summary>
        /// メッシュ数で数えたマップのサイズにヒットする正規表現オブジェクト
        /// </summary>
        private Regex size;
        /// <summary>
        /// DEM値にヒットする正規表現オブジェクト
        /// </summary>
        private Regex data;
        /* メソッド *****************************************************/
        /// <summary>
        /// 引数で渡されたテキストを走査してDEM情報クラスオブジェクトを返す
        /// <para>フォーマット不一致の場合はnew Info()を返します。</para>
        /// </summary>
        /// <param name="fileName">解析対象のファイルへのパス</param>
        /// <returns>正常に読み出せた場合はDEM情報クラスオブジェクトを返す</returns>
        public Info GetHeader(string fileName)                          // もう少しマッチの条件をスマートに書きたいんだけどなぁ。
        {
            if (System.IO.File.Exists(fileName) == false) return new Info();
            string txt = "";
            using (System.IO.StreamReader sr = new System.IO.StreamReader(fileName, System.Text.Encoding.GetEncoding("shift_jis")))
            {
                StringBuilder sb = new StringBuilder(2000);             // 予め大きなメモリ容量を確保しておく
                for (int i = 0; i < 50; i++)                            // ヘッダー部分だけに限ると、処理時間が半分になる。正規表現のマッチングが時間がかかっているみたいだ。
                    sb.Append(sr.ReadLine());
                txt = sb.ToString();
                //txt = sr.ReadToEnd();
            }

            string mapName = "";                                        // 地点名は2012/5/22時点では未対応
            int ID = 0;                                                 // ID番号
            Pos size = new Pos();                                       // マップサイズ（縦方向と横方向のメッシュ数）
            Blh upperRight = new Blh(), lowerLeft = new Blh();

            Match m;
            m = this.meshID.Match(txt);
            if (m.Success) ID = int.Parse(m.Groups["ID"].Value);
            m = this.lowerCorner.Match(txt);
            if (m.Success) upperRight = new Blh(double.Parse(m.Groups["lat"].Value), double.Parse(m.Groups["lon"].Value));
            m = this.upperCorner.Match(txt);
            if (m.Success) lowerLeft = new Blh(double.Parse(m.Groups["lat"].Value), double.Parse(m.Groups["lon"].Value));
            m = this.size.Match(txt);
            if (m.Success) size = new Pos(int.Parse(m.Groups["width"].Value) + 1, int.Parse(m.Groups["hight"].Value) + 1);

            RectangleField corner = new RectangleField(upperRight, lowerLeft);
            return new Info(mapName, ID, size, corner, Model.JPGIS2x_GML);
        }
        /// <summary>
        /// 引数で渡されたテキストを解析し、DEM値を読み出して返す
        /// </summary>
        /// <param name="fileName">解析したいファイルのパス</param>
        /// <returns>読み出されたDEM値（float型の一次元配列）</returns>
        public float[] GetValues(string fileName)
        {
            if (System.IO.File.Exists(fileName) == false) return null;
            string txt = "";
            using (System.IO.StreamReader sr = new System.IO.StreamReader(fileName, System.Text.Encoding.GetEncoding("shift_jis")))
            {
                txt = sr.ReadToEnd();
            }
            MatchCollection mc = this.data.Matches(txt);
            float[] buff = new float[mc.Count];
            Parallel.For(0, mc.Count, i =>
            {
                buff[i] = float.Parse(mc[i].Groups["value"].Value);
                if (buff[i] == -9999.0) buff[i] = float.NaN;
            });
            return buff;
        }
        /// <summary>
        /// マップのサイズ情報の支援を受けた上で、指定されたテキストよりDEM値を読み出して返す
        /// </summary>
        /// <param name="fileName">解析対象ファイルのパス</param>
        /// <param name="size">マップサイズ</param>
        /// <returns>読み出されたDEM値（float型の2次元配列）<para>読み出しに失敗するとnullを返します。</para></returns>
        public float[,] GetValues(string fileName, Pos size)
        {
            if (System.IO.File.Exists(fileName) == false) return null;
            /*
            string txt = "";
            using (System.IO.StreamReader sr = new System.IO.StreamReader(fileName, System.Text.Encoding.GetEncoding("shift_jis")))
            {
                txt = sr.ReadToEnd();
            }
            MatchCollection mc = this.data.Matches(txt);                        // 文字コードが合わなければヒットしない
            // デバッグコード
            //foreach (Match m in mc)
            //{
            //    Console.WriteLine(m.Groups["kind"].Value + m.Groups["value"].Value);
            //}
            
            if (mc.Count != size.x * size.y) return null;
            float[,] height = new float[size.x, size.y];                        // 配列サイズを調整
            Parallel.For(0, size.x * size.y, i =>
            //for(int i = 0; i < size.x * size.y; i++)
            {
                int k = i % size.x;
                int j = i / size.x;
                height[k, j] = float.Parse(mc[i].Groups["value"].Value);        // データを取得＆浮動小数点型の数値変換
                if (height[k, j] == -9999.0) height[k, j] = float.NaN;
            });
            */
            // 上の処理方法と比べると、下の処理は20倍くらい早い。正規表現によるマッチングがかなりボトルネックとなっていた。。。
            float[,] height = new float[size.x, size.y];                        // 配列サイズを調整
            int i = 0;
            using (System.IO.StreamReader sr = new System.IO.StreamReader(fileName, System.Text.Encoding.GetEncoding("shift_jis")))
            {
                while (sr.EndOfStream == false)
                {
                    string line = sr.ReadLine();
                    string[] field = line.Split(',');
                    if (field.Length == 2)
                    {
                        float value;
                        if (i >= size.x * size.y) return null;                  // 大きすぎる
                        if (float.TryParse(field[1], out value))                // 浮動小数点型の数値変換
                        {
                            int k = i % size.x;
                            int j = i / size.x;
                            if (value == -9999.0) value = float.NaN;
                            height[k, j] = value;                               // データを設定
                            i++;
                        }
                        
                    }
                }
            }
            if (i != size.x * size.y) return null;                              // サイズミスマッチ
            return height;
        }
        /// <summary>
        /// GMLフォーマットに一致するかチェックし、その結果を返す
        /// <para>2012/5/22　未実装　常にtrueを返すので役に立っていません。</para>
        /// </summary>
        /// <param name="txt">検査したいテキスト</param>
        /// <returns>true: マッチ</returns>
        public Boolean Match(ref string txt)
        {
            return true;
        }
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public JpgisGmlReader()
        {
            try
            {
                this.meshID = new Regex(@"<mesh>(?<ID>\d+)</mesh>");
                this.lowerCorner = new Regex(@"<gml:lowerCorner>(?<lat>\d{1,2}(?:[.]\d+)?) +(?<lon>\d{1,3}(?:[.]\d+)?) *</gml:lowerCorner>");
                this.upperCorner = new Regex(@"<gml:upperCorner>(?<lat>\d{1,2}(?:[.]\d+)?) +(?<lon>\d{1,3}(?:[.]\d+)?) *</gml:upperCorner>");
                this.size = new Regex(@"<gml:high>(?<width>\d+) +(?<hight>\d+)</gml:high>");
                this.data = new Regex(@"(?<kind>\w*)[,](?<value>-?\d{1,4}(?:[.]\d+)?)");
            }
            catch
            {
                throw new SystemException("JpgisGmlReaderクラスのコンストラクタにおいて、Regex（正規表現）オブジェクトの生成に失敗しました。");
            }
        }
    }
}
