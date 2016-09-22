using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MDSound.fmgen
{

    //	$Id: file.h,v 1.6 1999/11/26 10:14:09 cisc Exp $

    //#if !defined(win32_file_h)
    //#define win32_file_h

    //# include "types.h"

    public class FileIO
    {
        private Exception lastException = null;

        public enum Flags : int
        {
            Open = 0x000001,
            Readonly = 0x000002,
            Create = 0x000004,
        };

        public enum SeekMethod : int
        {
            begin = 0, current = 1, end = 2,
        };

        public enum Error
        {
            success = 0,
            file_not_found,
            sharing_violation,
            unknown = -1
        };

        // ---------------------------------------------------------------------------
        //	構築/消滅
        // ---------------------------------------------------------------------------

        public FileIO()
        {
            flags = 0;
        }

        public FileIO(string filename, uint flg = 0)
        {
            flags = 0;
            Open(filename, flg);
        }

        ~FileIO()
        {
            Close();
        }

        // ---------------------------------------------------------------------------
        //	ファイルを開く
        // ---------------------------------------------------------------------------

        public bool Open(string filename, uint flg = 0)
        {
            Close();

            path = filename;

            FileAccess access = (((flg & (uint)Flags.Readonly) > 0) ? FileAccess.ReadWrite : FileAccess.Write) | FileAccess.Read;
            FileShare share = ((flg & (uint)Flags.Readonly) > 0) ? FileShare.Read : FileShare.None;
            FileMode creation = ((flg & (uint)Flags.Create) > 0) ? FileMode.Create : FileMode.Open;

            try
            {
                hfile = new FileStream(filename, creation, access, share);
            }
            catch (System.IO.FileNotFoundException e)
            {
                lastException = e;
                error = Error.file_not_found;
                hfile = null;
            }
            catch (System.UnauthorizedAccessException e)
            {
                lastException = e;
                error = Error.sharing_violation;
                hfile = null;
            }
            catch (Exception e)
            {
                lastException = e;
                error = Error.unknown;
                hfile = null;
            }

            flags = (flg & (uint)Flags.Readonly) | ((hfile == null) ? 0 : (uint)(Flags.Open));
            //if ((flags & (uint)Flags.Open)==0)
            //{
            //    switch (GetLastError())
            //    {
            //        case ERROR_FILE_NOT_FOUND: error = file_not_found; break;
            //        case ERROR_SHARING_VIOLATION: error = sharing_violation; break;
            //        default: error = unknown; break;
            //    }
            //}
            SetLogicalOrigin(0);

            return (flags & (uint)Flags.Open) > 0;
        }

        // ---------------------------------------------------------------------------
        //	ファイルがない場合は作成
        // ---------------------------------------------------------------------------


        public bool CreateNew(string filename)
        {
            Close();

            path = filename;

            FileAccess access = FileAccess.ReadWrite;
            FileShare share = FileShare.None;
            FileMode creation = FileMode.CreateNew;

            try
            {
                hfile = new FileStream(filename, creation, access, share);
            }
            catch
            {
                hfile = null;
            }

            flags = (hfile == null ? 0 : (uint)(Flags.Open));
            SetLogicalOrigin(0);

            return (flags & (uint)Flags.Open) > 0;
        }

        // ---------------------------------------------------------------------------
        //	ファイルを作り直す
        // ---------------------------------------------------------------------------
        public bool Reopen(uint flg = 0)
        {
            if ((flags & (uint)Flags.Open) == 0) return false;
            if ((flags & (uint)Flags.Readonly) > 0 && (flg & (uint)Flags.Create) > 0) return false;

            if ((flags & (uint)Flags.Readonly) > 0) flg |= (uint)(Flags.Readonly);

            Close();

            FileAccess access = (((flg & (uint)Flags.Readonly) > 0) ? FileAccess.Read : FileAccess.Write) | FileAccess.Read;
            FileShare share = ((flg & (uint)Flags.Readonly) > 0) ? FileShare.Read : FileShare.None;
            FileMode creation = ((flg & (uint)Flags.Create) > 0) ? FileMode.Create : FileMode.Open;

            try
            {
                hfile = new FileStream(path, creation, access, share);
            }
            catch
            {
                hfile = null;
            }

            flags = (hfile == null ? 0 : (uint)(Flags.Open));
            SetLogicalOrigin(0);

            return (flags & (uint)Flags.Open) > 0;
        }

        // ---------------------------------------------------------------------------
        //	ファイルを閉じる
        // ---------------------------------------------------------------------------
        public void Close()
        {
            if ((GetFlags() & (uint)Flags.Open) > 0)
            {
                hfile.Close();
                flags = 0;
            }

        }

        public Error GetError()
        {
            return error;
        }

        // ---------------------------------------------------------------------------
        //	ファイル殻の読み出し
        // ---------------------------------------------------------------------------
        public Int32 Read(byte[] dest, Int32 len)
        {
            if ((GetFlags() & (uint)Flags.Open) == 0)
                return -1;

            int readsize;
            if ((readsize = hfile.Read(dest, 0, len)) == 0) return -1;

            return readsize;
        }

        // ---------------------------------------------------------------------------
        //	ファイルへの書き出し
        // ---------------------------------------------------------------------------
        public Int32 Write(byte[] src, Int32 len)
        {
            if (((GetFlags() & (uint)Flags.Open) == 0) || ((GetFlags() & (uint)Flags.Readonly) > 0))
                return -1;

            try
            {
                hfile.Write(src, 0, len);
            }
            catch
            {
                return -1;
            }

            return len;
        }

        // ---------------------------------------------------------------------------
        //	ファイルをシーク
        // ---------------------------------------------------------------------------
        public bool Seek(Int32 fpos, SeekMethod method)
        {
            if ((GetFlags() & (uint)Flags.Open) == 0)
                return false;

            SeekOrigin wmethod;
            switch (method)
            {
                case SeekMethod.begin:
                    wmethod = SeekOrigin.Begin;
                    fpos += (int)lorigin;
                    break;
                case SeekMethod.current:
                    wmethod = SeekOrigin.Current;
                    break;
                case SeekMethod.end:
                    wmethod = SeekOrigin.End;
                    break;
                default:
                    return false;
            }

            try
            {
                hfile.Seek(fpos, wmethod);
            }
            catch
            {
                return false;
            }

            return true;
        }

        // ---------------------------------------------------------------------------
        //	ファイルの位置を得る
        // ---------------------------------------------------------------------------
        public long Tellp()
        {
            if ((GetFlags() & (uint)Flags.Open) == 0)
                return 0;

            return hfile.Position;
        }

        // ---------------------------------------------------------------------------
        //	現在の位置をファイルの終端とする
        // ---------------------------------------------------------------------------
        public bool SetEndOfFile()
        {
            if ((GetFlags() & (uint)Flags.Open) == 0)
                return false;

            //未サポート
            //return ::SetEndOfFile(hfile) != 0;

            return true;
        }

        public uint GetFlags()
        {
            return flags;
        }

        public void SetLogicalOrigin(Int32 origin)
        {
            lorigin = (UInt32)origin;
        }

        private FileStream hfile;
        private uint flags;
        private UInt32 lorigin;
        private Error error;
        private string path = "";//[ MAX_PATH];

        //FileIO(const FileIO&);
        //const FileIO& operator=(const FileIO&);

    };

    //#endif // 

}