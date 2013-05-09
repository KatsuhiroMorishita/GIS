/**************************************************************************
 * GisTest.cs
 * GIS名前空間に属するクラス群をテストする静的クラス for C#
 * 
 * 開発者　：森下功啓（K.Morhista Kumamoto-University）
 * 開発履歴：
 *          2012/5/19   この頃、開発開始
 *          2012/5/28   ここ4日ほどを使って、SaveDemFileInfoAsKml()，SaveMapAsBmp(), SaveBigMapAsBmp(), ReadNmeaToRectangleField()を新設した。
 *          2012/6/6    ReadNmeaToKmlPoint()を新設した。時刻情報の付加もやりたいなぁ。
 *          2012/6/9    SaveDemFileInfoAsKml2()を新設した。
 *                      KMLのIcon情報を列挙体へ変更したのでReadNmeaToKmlPoint()を変更した。ついでに説明文の情報量を増やした。
 *          2012/6/10   DEMの動作テストメソッドとして、CombineGpsAndDemHeight()を実装した。
 *          2012/8/3    ReadNmeaToKmlPoint()は、ファイル選択をキャンセルしてもエラーとはなっていなかったが、
 *                      ファイルを開いたかどうかをチェックするように変更した。
 *          2012/8/7    一部のコメントを見直し
 *          2012/8/9    GNSS名前空間内にあったNMEA関係をTextData名前空間へ移した影響へ対応した。
 * ***********************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using GIS.DigitalElevationModel;
using GIS.DigitalElevationModel.Reader;
using GIS.MapBase;
using GIS.KML.Polygon;
using GIS.KML.Point;
using GNSS;
using GNSS.Field;
using GNSS.TextData.NMEA;


namespace GIS
{
    /// <summary>
    /// テスト専用の静的クラス
    /// </summary>
    public static class GisTest
    {
        /// <summary>
        /// フォルダを指定して、走査されたDEMファイルの一覧をKMLのポリゴンとしてファイルへ出力する
        /// <para>ReaderクラスとManagerクラスの動作テストです。</para>
        /// <para>ファイル名はDEMmaps.kmlです。</para>
        /// <para>異種類のDEMは先読みされた方が優先して処理され、後読みされた異種のDEMはファイル出力されませんのでご注意ください。</para>
        /// <example>
        /// <code>
        /// GIS.GisTest.SaveDemFileInfoAsKml(Color.MediumBlue);
        /// </code>
        /// </example>
        /// </summary>
        /// <param name="color">色情報（ポリゴンの色）</param>
        public static void SaveDemFileInfoAsKml(System.Drawing.Color color)
        {
            Reader reader = new Reader();
            var hoge = reader.ScanDirWithDialog(ReadMode.HeaderOnly);
            Manager man = new Manager();
            man.Add(hoge.Item1);
            using (System.IO.StreamWriter fwriter = new System.IO.StreamWriter("DEMmaps.kml", false, Encoding.UTF8))
            {
                try
                {
                    fwriter.Write(man.ToStringAsKml(color));            // 文字列化したデータを保存する。
                }
                catch
                {
                    // エラー処理があれば記述
                }
            }
            return;
        }
        /// <summary>
        /// フォルダを指定して、走査されたDEMファイルの一覧をKMLファイルへ出力する
        /// <para>DEMクラスのDEM読み込みテストです。</para>
        /// <para>全種類のDEMが種類毎にファイル出力されます。</para>
        /// <para>ファイル名はDEMmaps_nn.kmlです（nnは番号）。</para>
        /// <example>
        /// <code>
        /// GIS.GisTest.SaveDemFileInfoAsKml2();
        /// </code>
        /// </example>
        /// </summary>
        public static void SaveDemFileInfoAsKml2()
        {
            DEM dem = new DEM();
            dem.AddDirWithDialog();

            if (dem.Count != 0)
            {
                dem.SaveDemInfo("DEMmaps");
            }
            return;
        }
        /// <summary>
        /// NMEA形式のGPSのログから緯度経度を抜き出し、その地点における標高値をDEMより取得する
        /// <para>DEMクラスの動作テストです。</para>
        /// <para>GPSの高度測定精度の評価目的もあります。</para>
        /// </summary>
        public static void CombineGpsAndDemHeight()
        {
            DEM dem = new DEM();
            dem.AddDirWithDialog();

            if (dem.Count != 0)
            {
                NmeaReader reader = new NmeaReader();
                reader.OpenWithDialog();                                // NMEAファイル読み出し
                if (reader.IsOpen)
                {
                    var pos = reader.GetPositions();                    // 測位情報のうちから位置座標のみを取得
                    var times = reader.GetDateTimes();                  // 測位情報から時刻情報のみを取得
                    var height = dem.GetHeight(pos);
                    StringBuilder sb = new StringBuilder(5000);         // 予め大きなメモリ容量を確保しておく
                    sb.Append("Time,Longitude,Latitude,H,DEM height").Append(System.Environment.NewLine);
                    for (int i = 0; i < pos.Length && i < times.Length; i++)
                    {
                        if(pos[i].B != 0.0 && pos[i].L != 0.0)          // 比較が目的なので未測位データは除く
                            sb.Append(times[i].ToString()).Append(",").Append(pos[i].ToString()).Append(",").Append(height[i].ToString("0.0")).Append(System.Environment.NewLine);
                    }
                    using (System.IO.StreamWriter fwriter = new System.IO.StreamWriter("CombinedNmeaWithDem.csv", false, Encoding.UTF8))
                    {
                        try
                        {
                            fwriter.Write(sb.ToString());
                        }
                        catch
                        {
                            // エラー処理があれば記述
                        }
                    }
                }
            }
            return;
        }
        /// <summary>
        /// 読み込んだDEMファイルを一つだけビットマップファイルとして出力します
        /// <para>Readerクラスの読み込み機能及び、Mapクラスの画像出力機能のテストです。</para>
        /// <para>
        /// 出力するファイルは選べません。
        /// ファイルを全て読み込むので処理にかなりの時間がかかります。
        /// </para>
        /// <example>
        /// <code>
        /// GIS.GisTest.SaveMapAsBmp();
        /// </code>
        /// </example>
        /// </summary>
        public static void SaveMapAsBmp()
        {
            Reader reader = new Reader();
            var hoge = reader.ScanDirWithDialog(ReadMode.HeaderAndValue);
            var dem = hoge.Item1;
            if (dem.Count != 0)
            {
                if (dem[0].IsLoaded)
                {
                    DemSet demst = dem[0];
                    demst.DemMap.SaveAsImage("ToBmpTest.bmp", 0.0f);
                }
            }
            return;
        }
        /// <summary>
        /// DEMクラスの読み込みテストおよびマネジメント機能のテスト、さらにマップの結合テストを実施します
        /// <para>指定した座標で張る長方形領域を含む地図を生成します。</para>
        /// <para>座標を省略すると、Blh(33.0432, 130.559), Blh(32.6159, 130.9659)（熊本県付近）が使用されます。</para>
        /// <para>Taskクラスを利用して処理がすぐに戻るようにしておりますが、実際には若干の間をおいてファイルが生成されることにご注意ください。</para>
        /// </summary>
        /// <param name="x1">座標1</param>
        /// <param name="x2">座標2</param>
        public static void SaveBigMapAsBmp(Blh x1 = new Blh(), Blh x2 = new Blh())
        {
            DEM dem = new DEM();
            try
            {
                dem.AddDirWithDialog();
                if (dem.Count != 0)
                {
                    MapDem map = null;
                    var tsk = Task.Factory.StartNew(() =>
                    {
                        if (x1 == new Blh() && x1 == x2)
                            map = dem.CreateMap(new RectangleField(new Blh(33.0432, 130.559), new Blh(32.6159, 130.9659)));
                        else
                            map = dem.CreateMap(new RectangleField(x1, x2));

                        //System.Diagnostics.Stopwatch sw2 = System.Diagnostics.Stopwatch.StartNew();

                        if (map != null) map.SaveAsImage("ToBmpTest.bmp", 0.0f);

                        //sw2.Stop();                                                         // ストップウォッチを止める
                        //Console.WriteLine("マップ画像出力にかけた処理時間: " + sw2.Elapsed); // 結果を表示する
                    });
                }
            }
            catch (SystemException e)
            {
                Console.WriteLine(e.Message);
            }
            return;
        }
        /// <summary>
        /// RectangleFieldクラスとPolygonクラスのテスト
        /// <para>NMEAファイルからRectangleFieldオブジェクトを生成し、Polygonクラスを利用してKMLファイルを出力します。</para>
        /// <para>ダイアログを利用してファイルを開きます。</para>
        /// </summary>
        public static void ReadNmeaToRectangleField()
        {
            RectangleField field = GNSS.GnssTest.ReadNmeaToRectangleFeild();
            if (field.AreaIsZero == false)
            {
                Polygon polygon = new Polygon();
                polygon.AddToOuter(field.ToArray());                // KML出力に備え、座標をセット
                // ファイル出力
                // KML出力はUTF8で出力してください。
                using (System.IO.StreamWriter fwriter = new System.IO.StreamWriter("NMEAtoField.kml", false, Encoding.UTF8))
                {
                    try
                    {
                        fwriter.Write(polygon.GetKmlCode());
                    }
                    catch
                    {
                        // エラー処理があれば記述
                    }
                }
            }
            return;
        }
        /// <summary>
        /// Pointクラスのテスト
        /// <para>NMEAファイルから測位点情報を読み出し、KMLへPoint情報として出力します。</para>
        /// </summary>
        public static void ReadNmeaToKmlPoint()
        {
            NmeaReader reader = new NmeaReader();
            reader.OpenWithDialog();                                // NMEAファイル読み出し
            if (reader.IsOpen)
            {
                Blh[] pos = reader.GetPositions();                      // 測位情報のうちから位置座標のみを取得
                DateTime[] times = reader.GetDateTimes();               // 測位情報から時刻情報のみを取得

                if (pos.Length != 0 && times.Length != 0)
                {
                    Point point = new Point(pos[0], Icon.shaded_dot);
                    point.description = reader.FileName;
                    point.AddDescription("0");
                    point.AddDescription(pos[0].ToString());
                    point.AddDescription(times[0].ToString());
                    point.Time = new KML.Time.TimeStamp(times[0]);

                    if (pos.Length >= 1 && times.Length >= 1)
                    {
                        for (int i = 1; i < pos.Length && i < times.Length; i++)
                        {
                            if (!(pos[i].B == 0.0 && pos[i].L == 0.0))  // 未測位分は除く…最初の奴は例外（対処しようとすると結構長くなる）
                            {
                                Point p = new Point(pos[i]);
                                p.description = i.ToString();
                                p.AddDescription(pos[i].ToString());
                                p.AddDescription(times[i].ToString());
                                p.Time = new KML.Time.TimeStamp(times[i]);
                                point.Add(p);
                            }
                        }
                    }

                    // ファイル出力
                    // KML出力はUTF8で出力してください。
                    using (System.IO.StreamWriter fwriter = new System.IO.StreamWriter("NMEAtoPoint.kml", false, Encoding.UTF8))
                    {
                        try
                        {
                            fwriter.Write(point.GetKmlCode());
                        }
                        catch
                        {
                            // エラー処理があれば記述
                        }
                    }
                }
            }
            return;
        }
    }
}
