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
      state: '-',
      elapsed : 0
    },
    socket: new WebSocket(`ws://${new URL(window.location.href).host}/socket`),
    settings: {
      webcamUrl: null
    }
  },
  async beforeMount() {
    if (localStorage.getItem('settings')) {
      this.settings = JSON.parse(localStorage.getItem('settings'));
    }
    const response = await fetch("/api/serial", fetchOptionsFactory("GET"));
    if (response.status != 200) return;
    const devices = await response.json();
    if (devices) {
      this.ports.splice(0, this.ports.length);
      devices.map(device => {
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
      if (message.name === 'JobInfo') {
        appBeforeMount.statusPolling(message.data);
      }
    };

  },
  computed: {
    canControl: function () {
      return this.job.state === 'Stop' || this.job.state === 'Complete';
    },
    console: function () {
      return this.log
        .map((e) => {
          return `${e.timestamp} - ${e.message}\r\n`;
        });
    }    
  },
  methods: {
    connect: async function () {
      const response = await fetch(
        `/api/serial/${encodeURI(this.port)}`,
        fetchOptionsFactory(
          "POST",
          JSON.stringify({ baudRate: Number(this.baudRate), port: this.port })
        )
      );
      var result = await response.text();
      this.connected = response.status === 200;
      if (this.connected && result) {
        localStorage.setItem("port", this.port);
        localStorage.setItem("baudRate", this.baudRate);
      }
    },
    disconnect: async function () {
      if (confirm(`Disconnect from ${this.port}?`)) {
        const response = await fetch(
          `/api/serial/${encodeURI(this.port)}`,
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
      await this.sendCommand("G90 X0 Y0 Z0");
    },
    setHome: async function () {
      await this.sendCommand("G92 X0 Y0 Z0");
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
      const response = await fetch(
        `/api/cmnd`,
        fetchOptionsFactory("POST", JSON.stringify({ command: command, port: this.port }))
      );
      const message = await response.text();
      this.log.push({
        message: message.trim().trim('"'),
        timestamp: Date.now(),
      });
      if (this.log.length > 10) {
        this.log.shift();
      }
    },
    startJob: async function (file) {
      if (confirm(`Start ${file}?`)) {
        const response = await fetch(
          `/api/jobs/${file}`,
          fetchOptionsFactory("POST")
        );
        if (response.status === 200) {
          await response.text();
          this.job.percentage = 0;
        }
      }
    },
    stopJob: async function () {
      if (confirm(`Stop ${this.job.fileName}?`)) {
        this.job.status = 1;
        const response = await fetch(`/api/jobs/${this.job.fileName}`, fetchOptionsFactory("DELETE"));
        await response.text();
      }
    },
    pauseJob: async function () {
      const response = await fetch(`/api/jobs/${this.job.fileName}`, fetchOptionsFactory("PUT"));
      await response.text();
    },
    deleteFile: async function (file) {
      if (confirm(`Delete ${file}?`)) {
        const response = await fetch(`/api/jobs/${file}`, fetchOptionsFactory("DELETE"));
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
        await this.sendCommand(`G91 ${command}`);
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
      const response = await fetch(
        "/api/files",
        fetchOptionsFactory("GET")
      );
      const filesArray = await response.json();
      this.files.splice(0, this.files.length);
      this.files.push(...filesArray);
    },
    statusPolling(jobInfo) {
      this.job.percentage = jobInfo.percentage;
      this.job.state = jobInfo.state;
      this.job.fileName = jobInfo.fileName;
      this.job.elapsed = jobInfo.elapsed;
    }
  }
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