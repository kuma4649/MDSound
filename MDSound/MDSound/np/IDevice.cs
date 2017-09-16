using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MDSound.np
{
    public abstract class IDevice
    {
        public abstract void Reset();
        public abstract bool Write(UInt32 adr, UInt32 val, UInt32 id = 0);
        public abstract bool Read(UInt32 adr, ref UInt32 val, UInt32 id = 0);
        public abstract void SetOption(Int32 id, Int32 val);

    }

    public abstract class IRenderable : IDevice
    {
        /**
         * 音声のレンダリング
         * 
         * @param b[2] 合成されたデータを格納する配列．
         * b[0]が左チャンネル，b[1]が右チャンネルの音声データ．
         * @return 合成したデータのサイズ．1ならモノラル．2ならステレオ．0は合成失敗．
         */
        public abstract UInt32 Render(Int32[] b);

        /**
         *  chip update/operation is now bound to CPU clocks
         *  Render() now simply mixes and outputs sound
         */
        public abstract void Tick(UInt32 clocks);

    }

    /**
  * 音声合成チップ
  */
    public abstract class ISoundChip : IRenderable
    {
        /**
         * Soundchip clocked by M2 (NTSC = ~1.789MHz)
         */
        public abstract override void Tick(UInt32 clocks);

        /**
         * チップの動作クロックを設定
         *
         * @param clock 動作周波数
         */
        public abstract void SetClock(double clock);

        /**
         * 音声合成レート設定
         *
         * @param rate 出力周波数
         */
        public abstract void SetRate(double rate);

        /**
         * Channel mask.
         */
        public abstract void SetMask(int mask);

        /**
         * Stereo mix.
         *   mixl = 0-256
         *   mixr = 0-256
         *     128 = neutral
         *     256 = double
         *     0 = nil
         *    <0 = inverted
         */
        public abstract void SetStereoMix(int trk, Int16 mixl, Int16 mixr);

        /**
         * Track info for keyboard view.
         */
        //ITrackInfo GetTrackInfo(Int32 trk) { return null; }
    }

    public class Bus : IDevice
    {
        protected List<IDevice> vd = new List<IDevice>();

        /**
         * リセット
         *
         * <P>
         * 取り付けられている全てのデバイスの，Resetメソッドを呼び出す．
         * 呼び出し順序は，デバイスが取り付けられた順序に等しい．
         * </P>
         */
        public override void Reset()
        {

            foreach (IDevice it in vd)
                it.Reset();
        }

        /**
         * 全デバイスの取り外し
         */
        public void DetachAll()
        {
            vd.Clear();
        }

        /**
         * デバイスの取り付け
         *
         * <P>
         * このバスにデバイスを取り付ける．
         * </P>
         *
         * @param d 取り付けるデバイスへのポインタ
         */
        public void Attach(IDevice d)
        {
            vd.Add(d);
        }

        /**
         * 書き込み
         *
         * <P>
         * 取り付けられている全てのデバイスの，Writeメソッドを呼び出す．
         * 呼び出し順序は，デバイスが取り付けられた順序に等しい．
         * </P>
         */
        public override bool Write(UInt32 adr, UInt32 val, UInt32 id = 0)
        {
            bool ret = false;
            foreach (IDevice it in vd)
                ret |= it.Write(adr, val);
            return ret;
        }

        /**
         * 読み込み
         *
         * <P>
         * 取り付けられている全てのデバイスのReadメソッドを呼び出す．
         * 呼び出し順序は，デバイスが取り付けられた順序に等しい．
         * 帰り値は有効な(Readメソッドがtrueを返却した)デバイスの
         * 返り値の論理和．
         * </P>
         */
        public override bool Read(UInt32 adr, ref UInt32 val, UInt32 id = 0)
        {
            bool ret = false;
            UInt32 vtmp = 0;

            val = 0;
            foreach (IDevice it in vd)
            {
                if (it.Read(adr, ref vtmp))
                {
                    val |= vtmp;
                    ret = true;
                }
            }
            return ret;

        }

        public override void SetOption(int id, int val)
        {
            throw new NotImplementedException();
        }
    }

    /**
 * レイヤー
 *
 * <P>
 * バスと似ているが，読み書きの動作を全デバイスに伝播させない．
 * 最初に読み書きに成功したデバイスを発見した時点で終了する．
 * </P>
 */
    public class Layer : Bus
    {

        /**
         * 書き込み
         *
         * <P>
         * 取り付けられているデバイスのWriteメソッドを呼び出す．
         * 呼び出し順序は，デバイスが取り付けられた順序に等しい．
         * Writeに成功したデバイスが見つかった時点で終了．
         * </P>
         */
        public override bool Write(UInt32 adr, UInt32 val, UInt32 id = 0)
        {
            foreach (IDevice it in vd)
            {
                if (it.Write(adr, val)) return true;
            }

            return false;
        }

        /**
         * 読み込み
         *
         * <P>
         * 取り付けられているデバイスのReadメソッドを呼び出す．
         * 呼び出し順序は，デバイスが取り付けられた順序に等しい．
         * Readに成功したデバイスが見つかった時点で終了．
         * </P>
         */
        public override bool Read(UInt32 adr, ref UInt32 val, UInt32 id = 0)
        {
            val = 0;
            foreach (IDevice it in vd)
            {
                if (it.Read(adr, ref val)) return true;
            }

            return false;
        }
    }

}
