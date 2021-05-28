import { GcodeControllerApi } from "./api.js";
const api = GcodeControllerApi.initialize("/api/");
const app = new Vue({
  el: "#main",
  data: {
    port: "",
    ports: [],
    baudRate: 0,
    connected: false,
    commandHistory: [],
    log: [],
    xyStep: 5,
    zStep: 0.1,
    files: [],
    job: {
      fileName: "",
      percentage: 0,
      state: "-",
      elapsed: 0,
    },
    socket: new WebSocket(`ws://${new URL(window.location.href).host}/socket`),
    settings: {
      webcamUrl: null,
    },
  },
  async beforeMount() {
    if (localStorage.getItem("settings")) {
      this.settings = JSON.parse(localStorage.getItem("settings"));
    }
    const devices = await api.serial.list();
    if (devices) {
      this.ports.splice(0, this.ports.length);
      devices.map((device) => {
        if (device.isOpen) {
          this.port = device.port;
          this.baudRate = Number(device.baudRate);
          this.connected = device.isOpen;
        } else {
          this.ports.push(device.port);
        }
      });
      if (this.port.length < 1) {
        this.port = localStorage.getItem("port");
        this.baudRate = Number(localStorage.getItem("baudRate"));
      }
    }
    await this.getFiles();
    const appBeforeMount = this;
    this.socket.onmessage = function (event) {
      const message = JSON.parse(event.data);
      if (message.name === "JobInfo") {
        appBeforeMount.statusPolling(message.data);
      }
    };
  },
  computed: {
    canControl: function () {
      return this.job.state === "Stop" || this.job.state === "Complete";
    },
    console: function () {
      return this.log.map((e) => {
        return `${e.timestamp} - ${e.message}\r\n`;
      });
    },
  },
  methods: {
    connect: async function () {
      this.connected = await api.serial.post(this.baudRate, this.port);
      if (this.connected) {
        localStorage.setItem("port", this.port);
        localStorage.setItem("baudRate", this.baudRate);
      }
    },
    disconnect: async function () {
      if (confirm(`Disconnect from ${this.port}?`)) {
        this.connected = await api.serial.delete(this.port);
      }
    },
    clearConsole: function () {
      this.log.splice(0, this.log.length);
    },
    goHome: async function () {
      await api.command.post("G90 X0 Y0 Z0", this.port);
    },
    setHome: async function () {
      await api.command.post("G92 X0 Y0 Z0", this.port);
    },
    buttonSendCommand: async function (e) {
      await this.sendCommand(e.target.value);
    },
    saveSettings: function () {
      localStorage.setItem("settings", JSON.stringify(this.settings));
    },
    sendCommand: async function (command) {
      if (this.commandHistory.indexOf(command) >= 0) {
        this.commandHistory.push(command);
      }
      const result = await api.command.post(command, this.port);
      console.log(result);
      this.log.push({
        message: result,
        timestamp: Date.now(),
      });
      if (this.log.length > 10) {
        this.log.shift();
      }
    },
    startJob: async function (fileName) {
      if (confirm(`Start ${fileName}?`)) {
        const isStarted = await api.jobs.post(fileName);
        if (isStarted) {
          this.job.percentage = 0;
        } else {
          alert("Fail to start job");
        }
      }
    },
    stopJob: async function () {
      if (confirm(`Stop ${this.job.fileName}?`)) {
        this.job.status = 1;
        await api.jobs.delete();
      }
    },
    pauseJob: async function () {
      await api.jobs.put();
    },
    deleteFile: async function (file) {
      if (confirm(`Delete ${file}?`)) {
        await api.files.delete(file);
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
        await this.sendCommand(`G91 ${command}`);
      }
    },
    onPickFile() {
      this.$refs.fileInput.click();
    },
    async onFilePicked(event) {
      await api.files.post(event.target.files[0]);
      await this.getFiles();
    },
    async getFiles() {
      this.files.splice(0, this.files.length);
      this.files.push(...(await api.files.list()));
    },
    statusPolling(jobInfo) {
      this.job.percentage = jobInfo.percentage;
      this.job.state = jobInfo.state;
      this.job.fileName = jobInfo.fileName;
      this.job.elapsed = jobInfo.elapsed;
    },
  },
});

// const btn = document.getElementById("toggleTheme");
// const prefersDarkScheme = window.matchMedia("(prefers-color-scheme: dark)");

// const currentTheme = localStorage.getItem("theme");
// if (currentTheme == "dark") {
//   document.body.classList.toggle("dark-theme");
// } else if (currentTheme == "light") {
//   document.body.classList.toggle("light-theme");
// }

// btn.addEventListener("click", function () {
//   if (prefersDarkScheme.matches) {
//     document.body.classList.toggle("light-theme");
//     var theme = document.body.classList.contains("light-theme")
//       ? "light"
//       : "dark";
//   } else {
//     document.body.classList.toggle("dark-theme");
//     var theme = document.body.classList.contains("dark-theme")
//       ? "dark"
//       : "light";
//   }
//   localStorage.setItem("theme", theme);
// });
