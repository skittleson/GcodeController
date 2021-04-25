using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GcodeController.GcodeFirmwares {

    public interface IGcodeFirmware {

        bool EndOfCommand(string line);
        void Stop();
    }

    public abstract class GcodeFirmwareBase {

        public abstract bool EndOfCommand(string line);

        public abstract void Stop();

        // TODO move direction and units
        // TODO pause
        // TODO resume


    }
}
