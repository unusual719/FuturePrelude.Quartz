const FIXED_TOKEN_TEMPLATE = {
  needRequestToken: false,
  token: "",
  headerKey: "Authorization",
  prefix: "Bearer",
};

const DYNAMIC_TOKEN_TEMPLATE = {
  needRequestToken: true,
  tokenRequestUrl: "",
  tokenRequestMethod: "POST",
  tokenRequestHeaders: {},
  tokenRequestBody: {},
  tokenPath: "data.access_token",
  headerKey: "Authorization",
  prefix: "Bearer",
};

function cloneTemplate(template) {
  return JSON.parse(JSON.stringify(template));
}

export function createBearerAuthCredentialsTemplate(needRequestToken = false) {
  const template = needRequestToken
    ? cloneTemplate(DYNAMIC_TOKEN_TEMPLATE)
    : cloneTemplate(FIXED_TOKEN_TEMPLATE);

  return JSON.stringify(template, null, 2);
}

export function parseJsonText(text) {
  if (!String(text ?? "").trim()) {
    throw new Error("JSON 内容不能为空");
  }

  return JSON.parse(text);
}

export function formatJsonText(text) {
  return JSON.stringify(parseJsonText(text), null, 2);
}

export function detectNeedRequestToken(text) {
  try {
    const parsed = parseJsonText(text);
    return Boolean(parsed?.needRequestToken);
  } catch {
    return false;
  }
}
