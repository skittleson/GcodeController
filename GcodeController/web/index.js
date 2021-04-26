function fetchOptionsFactory(method, body = null) {
  return {
    method: method,
    headers: {
      "Content-Type": "application/json",
      cache: "no-store",
    },
    body: body
  };
}

function wait(time) {
  return new Promise((resolve) => {
    setTimeout(() => {
      resolve();
    }, time);
  });
}

const app = new Vue({
  el: "#main",
  data: {
    port: "",
    ports: [],
    baudRate: null,
    connected: false,
    log: [],
    xyStep: 5,
    zStep: 0.1,
    files: [],
    job: {
      fileName: "",
      percentage: 0,
      state: 0,
    },
  },
  async beforeMount() {
    const response = await fetch("/api/serial", fetchOptionsFactory("GET"));
    if (response.status === 200) {
      const device = await response.json();
      if (device && device.IsOpen) {
        this.port = device.Port;
        this.baudRate = device.BaudRate;
        this.connected = true;
      } else {
        this.ports = device.Ports;
        this.port = localStorage.getItem("port");
        this.baudRate = localStorage.getItem("baudRate");
      }
      await this.getFiles();
      await this.statusPolling();
    }
  },
  computed: {
    state: function () {
      switch (this.job.state) {
        case 0:
          return "Stopped";
        case 1:
          return "Stopping";
        case 2:
          return "Running";
        case 3:
          return "Pause";
        case 4:
          return "Complete";
        default:
          return "Unknown";
      }
    },
    canControl: function () {
      return this.job.state === 0 || this.job.state === 4;
    },
    console: function () {
      return this.log
        .slice()
        .reverse()
        .map((e) => {
          return `${e.timestamp} - ${e.message}\r\n`;
        });
    },
  },
  methods: {
    connect: async function () {
      const response = await fetch(
        "/api/serial",
        fetchOptionsFactory(
          "POST",
          JSON.stringify({ BaudRate: this.baudRate, Port: this.port })
        )
      );
      var result = await response.text();
      this.connected = response.status === 200;
      console.log(result);
      if (this.connected && result) {
        localStorage.setItem("port", this.port);
        localStorage.setItem("baudRate", this.baudRate);
      }
    },
    disconnect: async function () {
      if (confirm(`Disconnect from ${this.port}?`)) {
        const response = await fetch(
          "/api/serial",
          fetchOptionsFactory("DELETE")
        );
        await response.text();
        if (response.status === 200) {
          this.connected = false;
        }
      }
    },
    clearConsole: function () {
      this.log.splice(0, this.log.length);
    },
    goHome: async function () {
      await this.sendCommand({
        target: {
          value: "G90 X0 Y0 Z0",
        },
      });
    },
    setHome: async function () {
      await this.sendCommand({
        target: {
          value: "G92 X0 Y0 Z0",
        },
      });
    },
    sendCommand: async function (e) {
      const command = e.target.value;
      e.target.value = "";
      const response = await fetch(
        "/api/serial",
        fetchOptionsFactory("PUT", JSON.stringify({ Command: command }))
      );
      const json = await response.json();
      this.log.push({
        command: json.Command,
        message: json.Message,
        timestamp: json.Timestamp,
      });
      if (this.log.length > 10) {
        this.log.shift();
      }
    },
    startJob: async function (file) {
      const response = await fetch(
        "/api/job",
        fetchOptionsFactory("POST", JSON.stringify({ Name: file }))
      );
      await response.text();
      this.job.percentage = 0;
      await wait(500);
      await this.statusPolling();
    },
    stopJob: async function () {
      if (confirm(`Stop ${this.job.fileName}?`)) {
        this.job.status = 1;
        const response = await fetch("/api/job", fetchOptionsFactory("DELETE"));
        await response.text();
        await this.statusPolling();
      }
    },
    pauseJob: async function () {
      const response = await fetch("/api/job", fetchOptionsFactory("PUT"));
      await response.text();
      await this.statusPolling();
    },
    deleteFile: async function (file) {
      if (confirm(`Delete ${file}?`)) {
        const response = await fetch(
          `/api/files`,
          fetchOptionsFactory("DELETE", JSON.stringify({ Name: file }))
        );
        await response.text();
        await this.getFiles();
      }
    },
    jog: async function (e) {
      const buttonValue = e.target.value;
      let command = "";
      switch (buttonValue) {
        case "backward":
          command = `Y-${this.xyStep}`;
          break;
        case "forward":
          command = `Y${this.xyStep}`;
          break;
        case "left":
          command = `X-${this.xyStep}`;
          break;
        case "right":
          command = `X${this.xyStep}`;
          break;
        case "up":
          command = `Z${this.zStep}`;
          break;
        case "down":
          command = `Z-${this.zStep}`;
          break;
        default:
      }

      // TODO adding gcode in the ui seems like a bad idea
      if (command.length > 0) {
        const response = await fetch(
          "/api/serial",
          fetchOptionsFactory(
            "PUT",
            JSON.stringify({ Command: `G91 ${command}` })
          )
        );
        await response.text();
      }
    },
    onPickFile() {
      this.$refs.fileInput.click();
    },
    async onFilePicked(event) {
      //https://developer.mozilla.org/en-US/docs/Web/API/Fetch_API/Using_Fetch

      const files = event.target.files;
      const formData = new FormData();
      formData.append("gcode_file", files[0]);
      const response = await fetch("/api/files", {
        method: "POST",
        body: formData,
      });
      await response.text();
      await this.getFiles();
    },
    async getFiles() {
      const filesResponse = await fetch(
        "/api/files",
        fetchOptionsFactory("GET")
      );
      const filesArray = await filesResponse.json();
      this.files.splice(0, this.files.length);
      this.files.push(...filesArray);
    },
    async statusPolling() {
      const response = await fetch(
        `/api/job?percentage=${this.job.percentage}`,
        fetchOptionsFactory("GET")
      );
      const responseJson = await response.json();
      this.job.percentage = responseJson.Percentage;
      this.job.state = responseJson.State;
      this.job.fileName = responseJson.FileName;

      //long polling for status if running.
      if (this.job.state === 2) {
        await this.statusPolling();
      }
    },
  },
});
