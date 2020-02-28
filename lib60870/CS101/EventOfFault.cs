using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib60870.CS101
{
    /// <summary>
    /// Event of fault information object (M_FT_NA_1)
    /// </summary>
    public class EventOfFault : InformationObject
    {

        private byte remoteSignalCount;
        public byte RemoteSignalCount {
            get {
                return remoteSignalCount;
            }
        }

        private TypeID remoteSignalTI;
        public TypeID RemoteSignalTI {
            get {
                return remoteSignalTI;
            }
        }

        private List<SinglePointWithCP56Time2aOfFault> singleEvents;
        public List<SinglePointWithCP56Time2aOfFault> SingleEvents {
            get {
                return singleEvents;
            }
        }

        private byte remoteMeasureCount;
        public byte RemoteMeasureCount {
            get {
                return remoteMeasureCount;
            }
        }

        private TypeID remoteMeasureTI;
        public TypeID RemoteMeasureTI {
            get {
                return remoteMeasureTI;
            }
        }

        private List<InformationObject> measureValues;
        public List<InformationObject> MeasureValues {
            get {
                return measureValues;
            }
        }


        public EventOfFault(int ioa) : base(ioa)
        {
        }

        public EventOfFault(ApplicationLayerParameters parameters, byte[] msg, int startIndex) : base(parameters, msg, startIndex, false)
        {
            if ((msg.Length - startIndex) < GetEncodedSize())
                throw new ASDUParsingException("Message too small");

            remoteSignalCount = msg[startIndex++];
            remoteSignalTI = (TypeID)msg[startIndex++];

            singleEvents = new List<SinglePointWithCP56Time2aOfFault>();

            int elementSize = 8;

            for (int index = 0; index < remoteSignalCount; index++)
            {
                singleEvents.Add(GetSignalElement(parameters, msg, startIndex));

                startIndex += 2 + elementSize;
            }

            remoteMeasureCount = msg[startIndex++];
            remoteMeasureTI = (TypeID)msg[startIndex++];

            switch (RemoteMeasureTI)
            {
                case TypeID.M_ME_NA_1: /* 9 */

                    elementSize = 2;
                    break;
                case TypeID.M_ME_NC_1: /* 13 */

                    elementSize = 4;
                    break;
            }

            for (int index = 0; index < remoteMeasureCount; index++)
            {
                measureValues.Add(GetMeasureElement(parameters, msg, startIndex));

                startIndex += parameters.SizeOfIOA + elementSize;
            }
        }

        private SinglePointWithCP56Time2aOfFault GetSignalElement(ApplicationLayerParameters parameters, byte[] msg, int startIndex)
        {
            ApplicationLayerParameters m_parameters = parameters.Clone();
            m_parameters.SizeOfIOA = 2;

            var retVal = new SinglePointWithCP56Time2aOfFault(m_parameters, msg, startIndex, false);

            return retVal;
        }

        public InformationObject GetMeasureElement(ApplicationLayerParameters parameters, byte[] msg, int startIndex)
        {
            InformationObject retVal = null;

            switch (RemoteMeasureTI)
            {
                case TypeID.M_ME_NA_1: /* 9 */

                    retVal = new MeasuredValueNormalizedOfFault(parameters, msg, startIndex, false);

                    break;

                case TypeID.M_ME_NC_1: /* 13 */

                    retVal = new MeasuredValueShortOfFault(parameters, msg, startIndex, false);

                    break;

                default:
                    break;
            }

            return retVal;
        }

        public override bool SupportsSequence => false;

        public override TypeID Type => TypeID.M_FT_NA_1;

        public override void Encode(Frame frame, ApplicationLayerParameters parameters, bool isSequence)
        {
            base.Encode(frame, parameters, isSequence);
        }

        public override int GetEncodedSize()
        {
            //最小数据长度
            return 14 + 5 * 8;
            //return 14 + 7 * 8;
        }

    }

    public class SinglePointWithCP56Time2aOfFault : SinglePointInformation
    {
        override public int GetEncodedSize()
        {
            return 8;
        }

        override public TypeID Type {
            get {
                return TypeID.M_SP_TB_1;
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

        internal SinglePointWithCP56Time2aOfFault(ApplicationLayerParameters parameters, byte[] msg, int startIndex, bool isSequence)
            : base(parameters, msg, startIndex, isSequence)
        {
            if (!isSequence)
                startIndex += parameters.SizeOfIOA; /* skip IOA */

            if ((msg.Length - startIndex) < GetEncodedSize())
                throw new ASDUParsingException("Message too small");

            startIndex += 1; /* skip SIQ */

            /* parse CP56Time2a (time stamp) */
            timestamp = new CP56Time2a(msg, startIndex);
        }

        public SinglePointWithCP56Time2aOfFault(int objectAddress, bool value, QualityDescriptor quality, CP56Time2a timestamp)
            : base(objectAddress, value, quality)
        {
            this.timestamp = timestamp;
        }

        public override void Encode(Frame frame, ApplicationLayerParameters parameters, bool isSequence)
        {
            base.Encode(frame, parameters, isSequence);

            frame.AppendBytes(timestamp.GetEncodedValue());
        }
    }

    public class MeasuredValueNormalizedOfFault : MeasuredValueNormalizedWithoutQuality
    {
        override public int GetEncodedSize()
        {
            return 2;
        }

        override public TypeID Type {
            get {
                return TypeID.M_ME_NA_1;
            }
        }

        override public bool SupportsSequence {
            get {
                return true;
            }
        }

        public MeasuredValueNormalizedOfFault(int objectAddress, float value)
            : base(objectAddress, value)
        {

        }

        public MeasuredValueNormalizedOfFault(int objectAddress, short value)
            : base(objectAddress, value)
        {

        }

        internal MeasuredValueNormalizedOfFault(ApplicationLayerParameters parameters, byte[] msg, int startIndex, bool isSequence)
            : base(parameters, msg, startIndex, isSequence)
        {
            if (!isSequence)
                startIndex += parameters.SizeOfIOA; /* skip IOA */

            if ((msg.Length - startIndex) < GetEncodedSize())
                throw new ASDUParsingException("Message too small");

            startIndex += 2; /* normalized value */

        }

        public override void Encode(Frame frame, ApplicationLayerParameters parameters, bool isSequence)
        {
            base.Encode(frame, parameters, isSequence);

        }
    }

    public class MeasuredValueShortOfFault : InformationObject
    {
        override public int GetEncodedSize()
        {
            return 4;
        }

        override public TypeID Type {
            get {
                return TypeID.M_ME_NC_1;
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


        public MeasuredValueShortOfFault(int objectAddress, float value, QualityDescriptor quality)
            : base(objectAddress)
        {
            this.value = value;
        }


        internal MeasuredValueShortOfFault(ApplicationLayerParameters parameters, byte[] msg, int startIndex, bool isSequence)
            : base(parameters, msg, startIndex, isSequence)
        {
            if (!isSequence)
                startIndex += parameters.SizeOfIOA; /* skip IOA */

            if ((msg.Length - startIndex) < GetEncodedSize())
                throw new ASDUParsingException("Message too small");

            /* parse float value */
            value = System.BitConverter.ToSingle(msg, startIndex);
            startIndex += 4;

        }

        public override void Encode(Frame frame, ApplicationLayerParameters parameters, bool isSequence)
        {
            base.Encode(frame, parameters, isSequence);

            byte[] floatEncoded = BitConverter.GetBytes(value);

            if (BitConverter.IsLittleEndian == false)
                Array.Reverse(floatEncoded);

            frame.AppendBytes(floatEncoded);

        }
    }
}
