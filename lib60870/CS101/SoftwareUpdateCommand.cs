using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace lib60870.CS101
{
    /// <summary>
    /// Software update command (F_SR_NA_1_211)
    /// </summary>
    public class SoftwareUpdateCommand : InformationObject
    {
        override public int GetEncodedSize()
        {
            return 1;
        }

        override public TypeID Type {
            get {
                return TypeID.F_SR_NA_1_211;
            }
        }

        override public bool SupportsSequence {
            get {
                return false;
            }
        }

        private SetpointCommandQualifier qos;

        public SetpointCommandQualifier QOS {
            get {
                return qos;
            }
        }

        public SoftwareUpdateCommand(int objectAddress, SetpointCommandQualifier qos)
            : base(objectAddress)
        {
            this.qos = qos;
        }

        internal SoftwareUpdateCommand(ApplicationLayerParameters parameters, byte[] msg, int startIndex)
            : base(parameters, msg, startIndex, false)
        {
            startIndex += parameters.SizeOfIOA; /* skip IOA */

            if ((msg.Length - startIndex) < GetEncodedSize())
                throw new ASDUParsingException("Message too small");

            this.qos = new SetpointCommandQualifier(msg[startIndex++]);
        }

        public override void Encode(Frame frame, ApplicationLayerParameters parameters, bool isSequence)
        {
            base.Encode(frame, parameters, isSequence);

            frame.SetNextByte(this.qos.GetEncodedValue());
        }
    }
}
