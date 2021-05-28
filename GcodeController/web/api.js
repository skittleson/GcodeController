export class GcodeControllerApi {
  static initialize(apiUrl) {
    return new GcodeControllerApi(apiUrl);
  }
  constructor(apiUri) {
    this.serial = new SerialApi(apiUri + "serial");
    this.jobs = new JobsApi(apiUri + "jobs");
    this.command = new CommandApi(apiUri + "cmnd");
    this.files = new FilesApi(apiUri + "files");
  }
}

class SerialApi {
  constructor(apiUri) {
    this._apiUri = apiUri;
  }
  async post(baudRate, port) {
    const response = await fetch(
      `${this._apiUri}/${encodeURI(port)}`,
      fetchOptionsFactory(
        "POST",
        JSON.stringify({ baudRate: Number(baudRate), port: port })
      )
    );
    return await fetchResponseHandler(response, true);
  }
  async delete(port) {
    const response = await fetch(
      `${this._apiUri}/${encodeURI(port)}`,
      fetchOptionsFactory("DELETE")
    );
    return await fetchResponseHandler(response, true);
  }
  list = async () =>
    await fetchResponseHandler(
      await fetch(`${this._apiUri}`, fetchOptionsFactory("GET"))
    );
}

class CommandApi {
  constructor(apiUri) {
    this._apiUri = apiUri;
  }
  post = async (command, port) =>
    await fetchResponseHandler(
      await fetch(
        `${this._apiUri}`,
        fetchOptionsFactory(
          "POST",
          JSON.stringify({ command: command, port: port })
        )
      )
    );
}

class JobsApi {
  constructor(apiUri) {
    this._apiUri = apiUri;
  }
  post = async (file) =>
    await fetchResponseHandler(
      await fetch(`${this._apiUri}/${file}`, fetchOptionsFactory("POST")),
      true
    );
  delete = async () =>
    await fetchResponseHandler(
      await fetch(`${this._apiUri}`, fetchOptionsFactory("DELETE")),
      true
    );
  put = async () =>
    await fetchResponseHandler(
      await fetch(`${this._apiUri}`, fetchOptionsFactory("PUT")),
      true
    );
  list = async () =>
    await fetchResponseHandler(
      await fetch(`${this._apiUri}`, fetchOptionsFactory("GET"))
    );
}

class FilesApi {
  constructor(apiUri) {
    this._apiUri = apiUri;
  }

  /**
   * @example
   * // post(event.target.files[0]);
   * @external https://developer.mozilla.org/en-US/docs/Web/API/Fetch_API/Using_Fetch
   * @param {*} fileInput -  Single file from `event.target.files`
   * @returns {Boolean} - boolean on successful upload
   */
  async post(fileInput) {
    const formData = new FormData();
    formData.append("gcode_file", fileInput);
    const response = await fetch(this._apiUri, {
      method: "POST",
      body: formData,
    });
    return await fetchResponseHandler(response, true);
  }
  delete = async (file) =>
    await fetchResponseHandler(
      await fetch(`${this._apiUri}/${file}`, fetchOptionsFactory("DELETE")),
      true
    );
  list = async () =>
    await fetchResponseHandler(
      await fetch(`${this._apiUri}`, fetchOptionsFactory("GET"))
    );
}

function fetchOptionsFactory(method, body = null) {
  return {
    method: method,
    headers: {
      "Content-Type": "application/json",
      cache: "no-store",
    },
    body: body,
  };
}

async function fetchResponseHandler(response, returnIsSuccessful = false) {
  if (!response) throw new Error("Missing response");
  const contentType = response.headers.get("Content-Type");
  if (contentType.indexOf("json") != -1) {
    return await response.json();
  }
  const content = await response.text();
  if (returnIsSuccessful) {
    return response.status >= 200 || response.status < 300;
  }
  return content;
}
