using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace lib60870.CS101
{
    /// <summary>
    /// Integrated totals short (M_IT_NB_1 206)
    /// </summary>
    public class IntegratedTotalsShort : InformationObject
    {
        override public int GetEncodedSize()
        {
            return 5;
        }

        override public TypeID Type {
            get {
                return TypeID.M_IT_NB_1;
            }
        }

        override public bool SupportsSequence {
            get {
                return true;
            }
        }

        private float value;

        public float Value {
            get {
                return this.value;
            }
            set {
                this.value = value;
            }
        }

        private QualityDescriptor quality;

        public QualityDescriptor Quality {
            get {
                return this.quality;
            }
        }

        public IntegratedTotalsShort(int objectAddress, float value, QualityDescriptor quality)
            : base(objectAddress)
        {
            this.value = value;
            this.quality = quality;
        }


        internal IntegratedTotalsShort(ApplicationLayerParameters parameters, byte[] msg, int startIndex, bool isSequence)
            : base(parameters, msg, startIndex, isSequence)
        {
            if (!isSequence)
                startIndex += parameters.SizeOfIOA; /* skip IOA */

            if ((msg.Length - startIndex) < GetEncodedSize())
                throw new ASDUParsingException("Message too small");

            /* parse float value */
            value = System.BitConverter.ToSingle(msg, startIndex);
            startIndex += 4;

            /* parse QDS (quality) */
            quality = new QualityDescriptor(msg[startIndex++]);
        }

        public override void Encode(Frame frame, ApplicationLayerParameters parameters, bool isSequence)
        {
            base.Encode(frame, parameters, isSequence);

            byte[] floatEncoded = BitConverter.GetBytes(value);

            if (BitConverter.IsLittleEndian == false)
                Array.Reverse(floatEncoded);

            frame.AppendBytes(floatEncoded);

            frame.SetNextByte(quality.EncodedValue);
        }
    }


    /// <summary>
    /// Integrated totals short With CP56Time2a (M_IT_TC_1 207)
    /// </summary>
    public class IntegratedTotalsShortWithCP56Time2a : MeasuredValueShort
    {
        override public int GetEncodedSize()
        {
            return 12;
        }

        override public TypeID Type {
            get {
                return TypeID.M_IT_TC_1;
            }
        }

        override public bool SupportsSequence {
            get {
                return false;
            }
        }

        private CP56Time2a timestamp;

        public CP56Time2a Timestamp {
            get {
                return this.timestamp;
            }
        }

        public IntegratedTotalsShortWithCP56Time2a(int objectAddress, float value, QualityDescriptor quality, CP56Time2a timestamp)
            : base(objectAddress, value, quality)
        {
            this.timestamp = timestamp;
        }

        internal IntegratedTotalsShortWithCP56Time2a(ApplicationLayerParameters parameters, byte[] msg, int startIndex, bool isSequence)
            : base(parameters, msg, startIndex, isSequence)
        {
            if (!isSequence)
                startIndex += parameters.SizeOfIOA; /* skip IOA */

            if ((msg.Length - startIndex) < GetEncodedSize())
                throw new ASDUParsingException("Message too small");

            startIndex += 5; /* skip float */

            /* parse CP56Time2a (time stamp) */
            timestamp = new CP56Time2a(msg, startIndex);
        }

        public override void Encode(Frame frame, ApplicationLayerParameters parameters, bool isSequence)
        {
            base.Encode(frame, parameters, isSequence);

            frame.AppendBytes(timestamp.GetEncodedValue());
        }
    }
}
