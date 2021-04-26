namespace GcodeController.GcodeFirmwares {

    public interface IGcodeFirmware {

        bool EndOfCommand(string line);
        void Stop();

        bool IsBusy(string data);
    }

    public abstract class GcodeFirmwareBase : IGcodeFirmware {

        public abstract bool EndOfCommand(string line);

        public abstract void Stop();

        public abstract bool IsBusy(string data);
        // TODO move direction and units
        // TODO pause
        // TODO resume


    }
}
