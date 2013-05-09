/****************************************************************************
 * Manager.cs
 * DEMマップのマネージャ
 * DEMのファイル群を同じモデルごとに管理するためのクラスです。
 * 
 * 格納されるDEMはマップ原点を基準とした相対アドレスで管理されます（アドレスの考え方はメモ画像を参照）。
 * マップ原点は、マップを全球に敷き詰めたときに、緯度経度原点（緯度経度ともに0度）を含むマップの南西の角の座標とします。
 * もし西南の角の座標が緯度経度原点と一致すれば緯度経度原点をマップ原点として採用します。
 * 
 * [history]
 *      2012/5/21   開発開始
 *      2012/5/22   初期化・セット系の実装は終了した。あとは利用時のメソッドを実装する必要がある。
 *      2012/5/24   引数の型を一部で修正した。
 *                  ToString()を実装する。
 *      2012/5/25   高度取得系のメソッドを整備した。
 *      2012/5/27   CompareTo()を実装した。これでList<>でのソートができる。
 *      2012/6/6    KMLクラスの仕様変更に合わせて一部のコードを変更した。
 *      2012/6/9    PolygonクラスをBaseクラスを継承する形に改めたので、併せて一部のコードを変更した。
 *      2012/8/31   今日気が付いたが、2重登録を防止する機構が入っていないなぁ。いらないかな？
 * **************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GIS.MapBase;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;

using GNSS;
using GNSS.Field;
using GIS.KML.Polygon;
using Geodesy.GeodeticDatum;

namespace GIS.DigitalElevationModel
{
    /// <summary>
    /// DEMマネージャー
    /// <para>DEMのファイル群を同じモデルごとに管理するためのクラスです。</para>
    /// </summary>
    public class Manager : IComparable
    {
        //** メンバ変数***********************************************
        /// <summary>
        /// マップの管理ディクショナリ
        /// <para>マップの座標で管理します。</para>
        /// </summary>
        private Dictionary<Pos, DemSet> dict;
        /// <summary>
        /// マップ原点のオフセット量
        /// </summary>
        private Blh offset;
        /// <summary>
        /// 一つのマップに含まれているメッシュ量
        /// <para>メッシュ数を南北・東西方向の数で表しています。</para>
        /// </summary>
        private Pos amountOfMeshInMap;
        /// <summary>
        /// マップの大きさ
        /// <para>緯度経度[deg]で表した一つのマップの大きさ</para>
        /// </summary>
        private Blh mapSize;
        /// <summary>
        /// メッシュの大きさ
        /// <para>緯度経度[deg]で表した一つのメッシュの大きさ</para>
        /// </summary>
        private Blh meshSize;
        //** プロパティ **********************************************
        /// <summary>
        /// 保持しているマップ数
        /// </summary>
        public int Count
        {
            get {
                return this.dict.Count;
            }
        }
        /// <summary>
        /// 利用可能であるか返す
        /// <para>true: 利用可能</para>
        /// </summary>
        public Boolean Ready
        {
            get
            {
                if (this.Count == 0)
                    return false;
                else
                    return true;
            }
        }
        /// <summary>
        /// マップの大きさ
        /// <para>緯度経度[deg]で表した一つのマップの大きさ</para>
        /// </summary>
        public Blh MapSize
        {
            get
            {
                return this.mapSize;
            }
        }
        /// <summary>
        /// メッシュの大きさ
        /// <para>緯度経度[deg]で表した一つのメッシュの大きさ</para>
        /// </summary>
        public Blh MeshSize
        {
            get { 
                return this.meshSize; 
            }
        }
        //** メソッド ************************************************
        /// <summary>
        /// 自分自身がobjより小さいときはマイナスの数、大きいときはプラスの数、同じときは0を返す
        /// <para>MeshSizeの面積を用いて比較を行います。</para>
        /// <para>ListでのSortを意識しています。</para>
        /// </summary>
        /// <param name="obj">比較したいManagerクラスオブジェクト</param>
        /// <returns>大きいときはプラスの数、同じときは0</returns>
        public int CompareTo(object obj)
        {
            // nullより大きい
            if (obj == null)
            {
                return 1;
            }
            // 違う型とは比較できない
            if (this.GetType() != obj.GetType())
            {
                throw new ArgumentException("別の型とは比較できません。", "obj");
            }
            // MeshSizeを比較する
            return (this.MeshSize.B * this.MeshSize.L).CompareTo((((Manager)obj).MeshSize.B * ((Manager)obj).MeshSize.L)); ;
        }
        /// <summary>
        /// 指定された緯度経度の標高を返す
        /// <para>該当なしの場合は、float.NaNを返します</para>
        /// </summary>
        /// <param name="pos">指定座標</param>
        /// <returns>指定された座標のマップ情報を返す</returns>
        public float GetHeight(Blh pos)
        {
            Pos address = this.GetMapAddress(pos);
            if (this.dict.ContainsKey(address) == true)
            {
                if (this.dict[address].IsLoaded == false) this.dict[address].Load();    // まだデータを読み込んでいなければ読み込みを実施します。
                return this.dict[address].DemMap.GetValue(pos);
            }
            else
                return float.NaN;
        }
        /// <summary>
        /// 指定領域のカバー率を返す
        /// <para>指定された領域全てのDEMマップを保持している場合は1.0を返します。</para>
        /// </summary>
        /// <param name="field">指定領域</param>
        /// <returns>カバー率</returns>
        public double GetCoverRate(RectangleField field)
        {
            Pos upperRightPos = this.GetMapAddress(field.UpperRight);
            Pos lowerLeftPos = this.GetMapAddress(field.LowerLeft);

            double coverCount = 0;
            for (int x = lowerLeftPos.x; x <= upperRightPos.x; x++)
            {
                for (int y = lowerLeftPos.y; y <= upperRightPos.y; y++)
                {
                    Pos address = new Pos(x, y);
                    if (this.dict.ContainsKey(address) == true) coverCount += 1.0;
                }
            }
            return coverCount / (double)((upperRightPos.x - lowerLeftPos.x + 1) * (upperRightPos.y - lowerLeftPos.y + 1));
        }
        /// <summary>
        /// 指定領域のDEMを可能ならばロードする
        /// </summary>
        /// <param name="field">指定領域</param>
        public void Load(RectangleField field)
        {
            //System.Diagnostics.Stopwatch sw2 = System.Diagnostics.Stopwatch.StartNew();

            Pos upperRightPos = this.GetMapAddress(field.UpperRight);
            Pos lowerLeftPos = this.GetMapAddress(field.LowerLeft);
            /*
            for (int x = lowerLeftPos.x; x <= upperRightPos.x; x++)
            {
                for (int y = lowerLeftPos.y; y <= upperRightPos.y; y++)
                {
                    Pos address = new Pos(x, y);
                    if (this.dict.ContainsKey(address) == true)
                    {
                        if (this.dict[address].IsLoaded == false && this.dict[address].Error == false)
                        {
                            this.dict[address].Load();                          // まだデータを読み込んでいなければ読み込みを実施します。
                        }
                    }
                }
            }*/
            
            // 並列処理で書くならこうかく。あとで速度のテストをする予定。
           
            Parallel.For(lowerLeftPos.x, upperRightPos.x + 1, x =>              // 可能なら並列処理でデータを格納する
            {
                Parallel.For(lowerLeftPos.y, upperRightPos.y + 1, y =>
                {
                    Pos address = new Pos(x, y);
                    if (this.dict.ContainsKey(address) == true)
                    {
                        if (this.dict[address].IsLoaded == false && this.dict[address].Error == false)
                        {
                            this.dict[address].Load();                          // まだデータを読み込んでいなければ読み込みを実施します。
                        }
                    }
                });
            });

            //sw2.Stop();                                                         // ストップウォッチを止める
            //Console.WriteLine("マップの読み込みにかけた処理時間: " + sw2.Elapsed); // 結果を表示する
            return;
        }
        /// <summary>
        /// 指定されたマップアドレスから、そのアドレスのマップの領域情報を返す
        /// </summary>
        /// <param name="address">マップアドレス</param>
        /// <returns>領域</returns>
        private RectangleField GetFieldFromAddress(Pos address)
        {
            double lowerLeftLat = (double)address.y * this.mapSize.B + this.offset.B;
            double lowerLeftLon = (double)address.x * this.mapSize.L + this.offset.L;
            return new RectangleField(new Blh(lowerLeftLat, lowerLeftLon), new Blh(lowerLeftLat + this.mapSize.B, lowerLeftLon + this.mapSize.L));
        }
        /// <summary>
        /// 指定領域でマップを作成します
        /// <para>指定領域を含むマップを結合することでマップを生成します。</para>
        /// <para>該当データが存在しない領域はfloat.NaNで埋めます。</para>
        /// </summary>
        /// <param name="field">指定領域</param>
        /// <returns>Mapオブジェクト</returns>
        public MapDem CreateMap(RectangleField field)
        {
            this.Load(field);

            //System.Diagnostics.Stopwatch sw2 = System.Diagnostics.Stopwatch.StartNew();

            Pos upperRightPos = this.GetMapAddress(field.UpperRight);
            Pos lowerLeftPos = this.GetMapAddress(field.LowerLeft);
            Pos fieldMaxIndex = upperRightPos - lowerLeftPos;                                   // マップ単位でカウントした領域サイズ - 1
            Pos mapSize = new Pos((fieldMaxIndex.x + 1) * this.amountOfMeshInMap.x,
                (fieldMaxIndex.y + 1) * this.amountOfMeshInMap.y);                              // 生成するマップのサイズを決定する
            MapDem ansMap = new MapDem(new RectangleField(this.GetFieldFromAddress(lowerLeftPos).LowerLeft,
                this.GetFieldFromAddress(upperRightPos).UpperRight), mapSize);                  // マップを生成
            ansMap.Initialize(float.NaN);                                                       // 非値で初期化することで値が存在しない領域をはっきりさせる

            //int count = 0;
            Parallel.For(lowerLeftPos.x, upperRightPos.x + 1, x =>                              // 可能なら並列処理でデータを格納する
            //for (int x = lowerLeftPos.x; x < upperRightPos.x + 1; x++)
            {
                Parallel.For(lowerLeftPos.y, upperRightPos.y + 1, y =>
                //for (int y = lowerLeftPos.y; y < upperRightPos.y + 1; y++)
                {
                    Pos address = new Pos(x, y);
                    if (this.dict.ContainsKey(address) == true)
                    {
                        if (this.dict[address].Error == false && this.dict[address].IsLoaded)
                        {
                            //count++;
                            Pos relativeAddress = new Pos(x - lowerLeftPos.x, y - lowerLeftPos.y);
                            /*
                            for (int localX = 0; localX < this.amountOfMeshInMap.x; localX++)
                            {
                                for (int localY = 0; localY < this.amountOfMeshInMap.y; localY++)
                                {
                                    int globalX = relativeAddress.x * this.amountOfMeshInMap.x + localX;
                                    int globalY = (fieldMaxIndex.y - relativeAddress.y) * this.amountOfMeshInMap.y + localY;
                                    ansMap[globalX, globalY] = this.dict[address].DemMap[localX, localY];
                                }
                            }
                            */
                            Parallel.For(0, this.amountOfMeshInMap.x, localX =>
                            {
                                Parallel.For(0, this.amountOfMeshInMap.y, localY =>
                                {
                                    int globalX = relativeAddress.x * this.amountOfMeshInMap.x + localX;
                                    int globalY = (fieldMaxIndex.y - relativeAddress.y) * this.amountOfMeshInMap.y + localY;
                                    ansMap[globalX, globalY] = this.dict[address].map[localX, localY];
                                });
                            });
                        }
                    }
                });
            });

            //sw2.Stop();                                                             // ストップウォッチを止める
            //Console.WriteLine("マップの統合にかけた処理時間: " + sw2.Elapsed);    // 結果を表示する
            //Console.WriteLine("マップの結合数: " + count.ToString());
            return ansMap;
        }
        /// <summary>
        /// ハッシュ値を返す
        /// </summary>
        /// <returns>ハッシュ値</returns>
        public override int GetHashCode()
        {
            return this.amountOfMeshInMap.GetHashCode() ^ 
                this.mapSize.GetHashCode() ^ 
                this.offset.GetHashCode();
        }
        /// <summary>
        /// 保持している全DEM情報を文字列として返す
        /// </summary>
        /// <returns>DEM情報</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(5000);                 // 予め大きなメモリ容量を確保しておく
            var arr = this.dict.Values;
            foreach (DemSet member in arr)
            {
                string infoText = member.ToString();
                infoText = infoText.Replace(Environment.NewLine, "");   // 改行コードを削除
                sb.Append(infoText).Append(Environment.NewLine);
            }
            return sb.ToString();
        }
        /// <summary>
        /// 保持している全DEMデータ情報をKML形式のテキストデータで返す
        /// <para>ファイルへ保存する際はUTF-8で保存してください。</para>
        /// </summary>
        /// <param name="color">色情報</param>
        /// <returns>テキスト形式のKMLポリゴン情報</returns>
        public string ToStringAsKml(Color color)
        {
            Polygon polygon = new Polygon();
            polygon.name = "dummy";
            polygon.color = color;
            var arr = this.dict.Values.ToArray();

            /*
            if (arr.Length != 0)
            {
                for (int i = 1; i < arr.Length; i++)
                {
                    Polygon poly = new Polygon();
                    poly.name = arr[i].DataInfo.ID.ToString();
                    poly.AddToOuter(arr[i].DataInfo.Field.ToArray());
                    polygon.AddPolygon(poly);
                }
            }
             * */

            foreach (DemSet member in arr)
            {
                Polygon poly = new Polygon();
                poly.name = member.DataInfo.ID.ToString();
                poly.AddToOuter(member.DataInfo.Field.ToArray());
                StringBuilder sb = new StringBuilder(300);                      // 予め大きなメモリ容量を確保しておく
                sb.Append("File name: ").Append(member.FileName).Append(System.Environment.NewLine);
                sb.Append("Map Model: ").Append(member.DataInfo.MapModel.ToString()).Append(System.Environment.NewLine);
                sb.Append("Center position: ").Append(member.DataInfo.Field.Center.ToString());
                poly.description = sb.ToString();
                polygon.Add(poly);
            }
            return polygon.GetKmlCode();
        }
        /// <summary>
        /// 指定座標が含まれるマップのアドレスを返す
        /// </summary>
        /// <param name="pos">検査したい座標</param>
        /// <returns>マップのアドレス</returns>
        private Pos GetMapAddress(Blh pos)
        {
            pos.Unit = AngleUnit.Degree;                    // 単位を度に統一する
            pos.DatumKind = Datum.WGS84; // 測地系も統一
            pos -= offset;                                  // マップ原点を引いて、オフセットを取る
            int x = (int)(pos.L / this.mapSize.L);
            if (pos.L < 0.0) x -= 1;
            int y = (int)(pos.B / this.mapSize.B);
            if (pos.B < 0.0) y -= 1;
            return new Pos(x, y);
        }
        /// <summary>
        /// 指定座標の値を返すことができるか返す
        /// </summary>
        /// <param name="pos">指定座標</param>
        /// <returns>true: 値を返すことは可能</returns>
        public Boolean CheckAvailability(Blh pos)
        {
            Pos address = this.GetMapAddress(pos);
            if (this.dict.ContainsKey(address) == true)
                return true;
            else
                return false;
        }
        /// <summary>
        /// 値がゼロに近いかどうかを返す
        /// <para>緯度・経度にして約30cmの差を0とみなしたい。</para>
        /// <para>マップを地球に敷き詰めたときに端数を起因とする誤差を計算上吸収するために用いています。</para>
        /// </summary>
        /// <param name="value">検査したい値</param>
        /// <returns>true: ほぼゼロ</returns>
        private Boolean CheckNearlyZero(double value)
        {
            if (Math.Abs(value) < 0.000003)
                return true;
            else
                return false;
        }
        /// <summary>
        /// マップのオフセットを計算して返す
        /// <para>マップを世界中に敷き詰めたとしたとき、原点（緯度経度0度）を含むマップの南西にあるコーナー座標と原点との差をオフセットと定義しています。</para>
        /// </summary>
        /// <param name="info">マップの情報</param>
        /// <returns>オフセット量</returns>
        private Blh GetMapOffset(Info info)
        {
            Blh underLeft = info.Field.UpperLeft;
            underLeft.Unit = AngleUnit.Degree;                                      // 単位を度に統一する
            underLeft.DatumKind = Datum.WGS84;                   // 測地系も統一
            Blh size = info.Field.Size;
            size.Unit = AngleUnit.Degree;                                           // 単位を度に統一する
            size.DatumKind = Datum.WGS84;                        // 測地系も統一
            double remainderLat = underLeft.B % size.B;
            double remainderLon = underLeft.L % size.L;
            if (this.CheckNearlyZero(remainderLat)) remainderLat = 0.0;
            if (this.CheckNearlyZero(remainderLon)) remainderLon = 0.0;
            if (remainderLat != 0.0 && underLeft.B > 0.0) remainderLat -= size.B;   // 0なら一致している
            if (remainderLon != 0.0 && underLeft.L > 0.0) remainderLon -= size.L;
            if (this.CheckNearlyZero(remainderLat)) remainderLat = 0.0;
            if (this.CheckNearlyZero(remainderLon)) remainderLon = 0.0;
            if (this.CheckNearlyZero(remainderLat + size.B)) remainderLat = 0.0;    // 足したら0に近くなるようなら0と丸める
            if (this.CheckNearlyZero(remainderLon + size.L)) remainderLat = 0.0;
            return new Blh(remainderLat, remainderLon, 0.0, AngleUnit.Degree, Datum.WGS84);
        }
        /// <summary>
        /// 渡されたマップを用いて初期化する
        /// <para>呼び出すのは一回きりにしてください</para>
        /// </summary>
        /// <param name="info">マップの情報</param>
        private void Init(Info info)
        {
            if (this.dict.Count == 0)
            {
                this.mapSize = info.Field.Size;
                this.amountOfMeshInMap = info.Size;
                this.meshSize = new Blh(this.mapSize.B / (double)this.amountOfMeshInMap.y, this.mapSize.L / (double)this.amountOfMeshInMap.x);
                this.offset = this.GetMapOffset(info);
            }
            return;
        }
        /// <summary>
        /// 2つのBlh座標について、小数点以下digit桁の精度で一致しているかどうか検査する
        /// <para>8桁としていて一致しているとみなされた場合、最大でも1.2 mm程度の誤差しか有りません。</para>
        /// </summary>
        /// <param name="x1">検査したい座標1</param>
        /// <param name="x2">検査したい座標2</param>
        /// <param name="digit">桁数</param>
        /// <returns>true: 緯度経度は一致しているとみなせる</returns>
        private Boolean ComparePosition(Blh x1, Blh x2, int digit)
        {
            long xb1 = (long)(Math.Round(x1.B * Math.Pow(10.0, (double)digit)));
            long xb2 = (long)(Math.Round(x2.B * Math.Pow(10.0, (double)digit)));
            long xl1 = (long)(Math.Round(x1.L * Math.Pow(10.0, (double)digit)));
            long xl2 = (long)(Math.Round(x2.L * Math.Pow(10.0, (double)digit)));
            return (xb1 == xb2 && xl1 == xl2);
        }
        /// <summary>
        /// マップをセットする
        /// <para>
        /// 二重登録は致しません。
        /// 最初に登録されたマップの情報を用いてManagerオブジェクトを定義します。
        /// 2つ目以降のマップは、同系統とみなされなかった場合に登録を拒否されます。
        /// </para>
        /// </summary>
        /// <param name="map">マップオブジェクト</param>
        /// <returns>セットに成功するとtureを返す</returns>
        public Boolean Add(DemSet map)
        {
            if (this.dict.Count == 0) this.Init(map.DataInfo);
            //Blh debug = this.GetMapOffset(map.DataInfo);
            if (this.ComparePosition(map.DataInfo.Field.Size, this.mapSize, 8) &&
                map.DataInfo.Size == this.amountOfMeshInMap &&
                ComparePosition(this.GetMapOffset(map.DataInfo), this.offset, 6))
            {
                Pos address = this.GetMapAddress(map.DataInfo.Field.Center);
                if (this.dict.ContainsKey(address) == false) this.dict.Add(address, map);
                return true;
            }
            else
                return false;
        }
        /// <summary>
        /// Listを使ってマップを追加します
        /// <para>
        /// 引数で渡したリストは加工され、本オブジェクトに吸収されなかったものだけが残されるようにしています。
        /// この副作用にはご注意ください。
        /// </para>
        /// </summary>
        /// <param name="maps">追加したいマップ</param>
        /// <returns>本オブジェクトに追加されなかったマップをListを使って返します。</returns>
        public List<DemSet> Add(List<DemSet> maps)
        { 
            int i = 0;
            while (maps.Count != 0 && i < maps.Count)
            {
                Boolean result = this.Add(maps[i]);
                if (result == false)
                    i += 1;
                else
                    maps.RemoveAt(i);
            }
            return maps;
        }
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public Manager()
        {
            this.dict = new System.Collections.Generic.Dictionary<Pos, DemSet>();
            this.meshSize = new Blh();
        }
    }
    
}
