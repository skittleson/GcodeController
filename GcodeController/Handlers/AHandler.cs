namespace GcodeController.Handlers {

    public interface IHandler {
        string GetPrefix {
            get;
        }
    }
    public abstract class AHandler : IHandler {
        public abstract string GetPrefix {
            get;
        }
    }
}
