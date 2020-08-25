using System;

namespace ZLIB
{
	public enum FLevel
    { 
        Faster = 0,
        Fast = 1,
        Default = 2,
        Optimal = 3,
    }
    public sealed class ZLibHeader
    {
        #region "Variables globales"
        private bool mIsSupportedZLibStream;
        private byte mCompressionMethod; //CMF 0-3
        private byte mCompressionInfo; //CMF 4-7
        private byte mFCheck; //Flag 0-4 (Check bits for CMF and FLG)
        private bool mFDict; //Flag 5 (Preset dictionary)
        private FLevel mFLevel; //Flag 6-7 (Compression level)
        #endregion
        #region "Propiedades"
        public bool IsSupportedZLibStream
        {
            get
            {
                return this.mIsSupportedZLibStream;
            }
            set
            {
                this.mIsSupportedZLibStream = value;
            }
        }
        public byte CompressionMethod
        {
            get
            {
                return this.mCompressionMethod;
            }
            set
            {
                if (value > 15) 
                { 
                    throw new ArgumentOutOfRangeException("Argument cannot be greater than 15");
                }
                this.mCompressionMethod = value;
            }
        }
        public byte CompressionInfo
        {
            get
            {
                return this.mCompressionInfo;
            }
            set
            {
                if (value > 15)
                {
                    throw new ArgumentOutOfRangeException("Argument cannot be greater than 15");
                }
                this.mCompressionInfo = value;
            }
        }
        public byte FCheck 
        {
            get
            {
                return this.mFCheck;
            }
            set
            {
                if (value > 31)
                {
                    throw new ArgumentOutOfRangeException("Argument cannot be greater than 31");
                }
                this.mFCheck = value;
            }
        }
        public bool FDict 
        {
            get
            {
                return this.mFDict;
            }
            set
            {
                this.mFDict = value;
            }
        }
        public FLevel FLevel 
        {
            get
            {
                return this.mFLevel;
            }
            set
            {
                this.mFLevel = value;
            }
        }
        #endregion
        #region "Constructor"
            public ZLibHeader() 
            {

            }
        #endregion
        #region "Metodos privados"
        private void RefreshFCheck()
        {
			byte byteFLG = 0x00;

			byteFLG = (byte)(Convert.ToByte(this.FLevel) << 1);
			byteFLG |= Convert.ToByte(this.FDict);

            this.FCheck = Convert.ToByte(31 - Convert.ToByte((this.GetCMF() * 256 + byteFLG) % 31));
        }
        private byte GetCMF()
        {
			byte byteCMF = 0x00;

			byteCMF = (byte)(this.CompressionInfo << 4);
			byteCMF |= (byte)(this.CompressionMethod);

			return byteCMF;
		}
        private byte GetFLG()
        {
			byte byteFLG = 0x00;

			byteFLG = (byte)(Convert.ToByte(this.FLevel) << 6);
			byteFLG |= (byte)(Convert.ToByte(this.FDict) << 5);
			byteFLG |= this.FCheck;

			return byteFLG;
        }
        #endregion
        #region "Metodos publicos"
        public byte[] EncodeZlibHeader() 
        {
            byte[] result = new byte[2];

            this.RefreshFCheck();

            result[0] = this.GetCMF();
            result[1] = this.GetFLG();

            return result;
        }
        #endregion
        #region "Metodos estáticos"
        public static ZLibHeader DecodeHeader(int pCMF, int pFlag)
        {
            ZLibHeader result = new ZLibHeader();

			//Ensure that parameters are bytes
			pCMF = pCMF & 0x0FF;
			pFlag = pFlag & 0x0FF;

			//Decode bytes
			result.CompressionInfo = Convert.ToByte((pCMF & 0xF0) >> 4);
			result.CompressionMethod = Convert.ToByte(pCMF & 0x0F);

			result.FCheck = Convert.ToByte(pFlag & 0x1F);
			result.FDict = Convert.ToBoolean(Convert.ToByte((pFlag & 0x20) >> 5));
			result.FLevel = (FLevel)Convert.ToByte((pFlag & 0xC0) >> 6);

			result.IsSupportedZLibStream = (result.CompressionMethod == 8) && (result.CompressionInfo == 7) && (((pCMF * 256 + pFlag) % 31 == 0)) && (result.FDict == false);

            return result;
        }
        #endregion
    }
}
