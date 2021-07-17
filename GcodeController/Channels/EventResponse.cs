using System.Text;

namespace GcodeController.Channels {
    public class EventResponse<T> {
        public EventResponse(T data) {
            Data = data;
        }
        public string Name => typeof(T).Name;
        public T Data {
            get; private set;
        }
        public override string ToString() {

            //TODO serializer defaults
            return System.Text.Json.JsonSerializer.Serialize(this, Utils.JsonOptions());
        }
        public byte[] ToBytes() => Encoding.UTF8.GetBytes(ToString());

        public static EventResponse<T> New(T data) => new EventResponse<T>(data);
        public static string Payload(T data) => (new EventResponse<T>(data)).ToString();

    }
}
