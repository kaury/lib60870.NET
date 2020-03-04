using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace lib60870.CS101
{
    /// <summary>
    /// C_SR_NA_1 200
    /// </summary>
    public class SwitchSettingArea : InformationObject
    {

        override public int GetEncodedSize()
        {
            return 2;
        }

        override public TypeID Type {
            get {
                return TypeID.C_SR_NA_1;
            }
        }

        override public bool SupportsSequence {
            get {
                return false;
            }
        }

        private ushort sn;

        public ushort SN {
            get {
                return sn;
            }
        }

        public SwitchSettingArea(int objectAddress, ushort sn)
            : base(objectAddress)
        {
            this.sn = sn;
        }

        internal SwitchSettingArea(ApplicationLayerParameters parameters, byte[] msg, int startIndex)
            : base(parameters, msg, startIndex, false)
        {
            startIndex += parameters.SizeOfIOA; /* skip IOA */

            if ((msg.Length - startIndex) < GetEncodedSize())
                throw new ASDUParsingException("Message too small");

            this.sn = BitConverter.ToUInt16(msg, startIndex++);
        }

        public override void Encode(Frame frame, ApplicationLayerParameters parameters, bool isSequence)
        {
            base.Encode(frame, parameters, isSequence);

            frame.SetNextByte((byte)(this.sn & 0xff));
            frame.SetNextByte((byte)(this.sn / 0x100 & 0xff));
        }
    }

    /// <summary>
    /// C_RR_NA_1 201
    /// </summary>
    public class ReadCurrentSettingArea : InformationObject
    {
        private ushort sn1;
        public ushort SNCurrent {
            get { return sn1; }
        }
        private ushort sn2;
        public ushort SNMin {
            get { return sn2; }
        }
        private ushort sn3;
        public ushort SNMax {
            get { return sn3; }
        }

        public ReadCurrentSettingArea() : base(0)
        {
        }

        public ReadCurrentSettingArea(ApplicationLayerParameters parameters, byte[] msg, int startIndex) : base(parameters, msg, startIndex, false)
        {
            startIndex += parameters.SizeOfIOA;

            if ((msg.Length - startIndex) < GetEncodedSize())
                throw new ASDUParsingException("Message too small");

            sn1 = BitConverter.ToUInt16(msg, startIndex);
            startIndex += 2;
            sn2 = BitConverter.ToUInt16(msg, startIndex);
            startIndex += 2;
            sn3 = BitConverter.ToUInt16(msg, startIndex);
        }

        public override bool SupportsSequence => false;

        public override TypeID Type => TypeID.C_RR_NA_1;

        public override int GetEncodedSize()
        {
            return 6;
        }
    }

    /// <summary>
    /// C_RS_NA_1 202
    /// </summary>
    public class ReadParameters : InformationObject
    {
        private ushort _sn;
        public ushort SN {
            get { return _sn; }
        }
        private int[] _ioas;

        private ParameterIdentification _pi;
        public ParameterIdentification PI {
            get { return _pi; }
        }

        private List<Parameter> _parameters;
        public List<Parameter> Parameters {
            get { return _parameters; }
        }

        public ReadParameters(ushort sn) : base(0)
        {
            _sn = sn;
            _ioas = null;
        }

        public ReadParameters(ushort sn, int[] ioas) : base(0)
        {
            _sn = sn;
            _ioas = ioas;
        }

        internal ReadParameters(ApplicationLayerParameters parameters, byte[] msg, int startIndex, int elementCount) : base(parameters, msg, startIndex, true)
        {
            _sn = BitConverter.ToUInt16(msg, startIndex);
            startIndex += 2;

            _pi = new ParameterIdentification(msg[startIndex++]);

            if ((msg.Length - startIndex) < GetEncodedSize())
                throw new ASDUParsingException("Message too small");

            _parameters = new List<Parameter>();
            for (int i = 0; i < elementCount; i++)
            {
                var param = new Parameter();

                param.ioa = msg[startIndex++];

                if (parameters.SizeOfIOA > 1)
                    param.ioa += (msg[startIndex++] * 0x100);

                if (parameters.SizeOfIOA > 2)
                    param.ioa += (msg[startIndex++] * 0x10000);

                param.Tag = msg[startIndex++];

                param.Length = msg[startIndex++];

                param.RawBytes = new byte[param.Length];
                if (param.Length > 0)
                {
                    Array.Copy(msg, startIndex, param.RawBytes, 0, param.Length);
                    startIndex += param.Length;
                }

                _parameters.Add(param);
            }
        }

        public override bool SupportsSequence => false;

        public override TypeID Type => TypeID.C_RS_NA_1;

        public override void Encode(Frame frame, ApplicationLayerParameters parameters, bool isSequence)
        {
            frame.SetNextByte((byte)(_sn & 0xff));
            frame.SetNextByte((byte)((_sn / 0x100) & 0xff));
            if (_ioas != null)
            {
                foreach (var item in _ioas)
                {
                    frame.SetNextByte((byte)(_sn & 0xff));
                    frame.SetNextByte((byte)((_sn / 0x100) & 0xff));
                    frame.SetNextByte((byte)((_sn / 0x10000) & 0xff));
                }
            }
        }

        public override int GetEncodedSize()
        {
            return 5;
        }
    }

    /// <summary>
    /// C_WS_NA_1 203
    /// </summary>
    public class WriteParameters : InformationObject
    {
        private ushort _sn;
        public ushort SN {
            get { return _sn; }
        }

        private ParameterIdentification _pi;
        public ParameterIdentification PI {
            get { return _pi; }
        }

        private Parameter[] _parameters;

        public WriteParameters(ushort sn, ParameterIdentification pi, Parameter[] parameters) : base(0)
        {
            _sn = sn;
            _pi = pi;
            _parameters = parameters;
        }

        public WriteParameters(ApplicationLayerParameters parameters, byte[] msg, int startIndex) : base(parameters, msg, startIndex, true)
        {
            if ((msg.Length - startIndex) < GetEncodedSize())
                throw new ASDUParsingException("Message too small");

            _sn = BitConverter.ToUInt16(msg, startIndex);
            startIndex += 2;

            _pi = new ParameterIdentification(msg[startIndex++]);

        }

        public override bool SupportsSequence => false;

        public override TypeID Type => TypeID.C_WS_NA_1;

        public override int GetEncodedSize()
        {
            return 3;
        }

        public override void Encode(Frame frame, ApplicationLayerParameters parameters, bool isSequence)
        {
            frame.SetNextByte((byte)(_sn & 0xff));
            frame.SetNextByte((byte)((_sn / 0x100) & 0xff));

            frame.SetNextByte(_pi.GetEncodedValue());

            foreach (var item in _parameters)
            {
                frame.SetNextByte((byte)(item.ioa & 0xff));
                frame.SetNextByte((byte)((item.ioa / 0x100) & 0xff));
                frame.SetNextByte((byte)((item.ioa / 0x10000) & 0xff));

                frame.SetNextByte(item.Tag);

                frame.SetNextByte(item.Length);

                frame.AppendBytes(item.RawBytes);
            }

        }
    }

    /// <summary>
    /// 参数特征标识 PI
    /// </summary>
    public class ParameterIdentification
    {

        private byte encodedValue;

        public ParameterIdentification(byte encodedValue)
        {
            this.encodedValue = encodedValue;
        }

        public bool Followup {
            get {
                if ((encodedValue & 0x01) != 0)
                    return true;
                else
                    return false;
            }

            set {
                if (value)
                    encodedValue |= 0x01;
                else
                    encodedValue &= 0xfe;
            }
        }

        public byte RES {
            get {
                return (byte)((encodedValue & 0x3e) / 2);
            }
            set {
                encodedValue |= (byte)((value & 0x1f) * 2);
            }
        }

        public bool CR {
            get {
                return ((encodedValue & 0x40) == 0x40);
            }

            set {
                if (value)
                    encodedValue |= 0x40;
                else
                    encodedValue &= 0xbf;
            }
        }
        /// <summary>
        /// false: 0: 固化 ，true: 1: 预置 
        /// </summary>
        public bool Select {
            get {
                return ((encodedValue & 0x80) == 0x80);
            }

            set {
                if (value)
                    encodedValue |= 0x80;
                else
                    encodedValue &= 0x7f;
            }
        }

        public byte GetEncodedValue()
        {
            return encodedValue;
        }
    }

    public class Parameter
    {
        public int ioa;
        public byte Tag;
        public byte Length;
        /// <summary>
        /// 不知道类型，暂存原始字节
        /// </summary>
        public byte[] RawBytes;
    }
}
