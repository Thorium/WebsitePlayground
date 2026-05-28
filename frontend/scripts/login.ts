let loginAttempts = 0;
let lastLoginAttempt = 0;
const maxAttemptsPerMinute = 5;
const attemptWindowMs = 60000;

type AuthResponse = {
    success?: boolean;
    Success?: boolean;
    errorCode?: string;
    ErrorCode?: string;
    errorMessage?: string;
    ErrorMessage?: string;
    lockedUntil?: string;
    LockedUntil?: string;
};

function postJson(url: string, payload: any): Promise<AuthResponse> {
    return fetch(url, {
        method: "POST",
        credentials: "same-origin",
        headers: {
            "Content-Type": "application/json"
        },
        body: JSON.stringify(payload)
    }).then((response) => {
        if (!response.ok) {
            throw new Error(`Request failed with status ${response.status}`);
        }
        return response.json();
    });
}

export function getJson(url: string): Promise<any> {
    return fetch(url, {
        method: "GET",
        credentials: "same-origin"
    }).then((response) => {
        if (!response.ok) {
            throw new Error(`Request failed with status ${response.status}`);
        }
        return response.json();
    });
}

function checkRateLimit(): boolean {
    const now = Date.now();
    if (now - lastLoginAttempt > attemptWindowMs) {
        loginAttempts = 0;
    }

    if (loginAttempts >= maxAttemptsPerMinute) {
        const timeLeft = Math.ceil((attemptWindowMs - (now - lastLoginAttempt)) / 1000);
        showError("loginError", "loginErrorMessage", `Too many login attempts. Please wait ${timeLeft} seconds before trying again.`);
        return false;
    }

    loginAttempts++;
    lastLoginAttempt = now;
    return true;
}

function showError(containerId: string, messageId: string, message: string): void {
    $(`#${messageId}`).text(message);
    $(`#${containerId}`).show();
    setTimeout(() => {
        $(`#${containerId}`).fadeOut();
    }, 5000);
}

function showSuccess(containerId: string): void {
    $(`#${containerId}`).show();
}

function showTransientSuccess(containerId: string, timeoutMs: number): void {
    $(`#${containerId}`).show();
    setTimeout(() => {
        $(`#${containerId}`).fadeOut();
    }, timeoutMs);
}

function activateTab(tabName: string): void {
    $(".login-tab-button").removeClass("active");
    $(`.login-tab-button[data-auth-tab='${tabName}']`).addClass("active");
    $(".auth-tab-panel").removeClass("active");
    if (tabName === "register") {
        $("#registerTab").addClass("active");
    } else {
        $("#loginTab").addClass("active");
    }
}

function resetLoginButton(): void {
    $("#loginButton").prop("disabled", false).html('<span class="fa fa-sign-in"></span> Login');
}

function resetRegisterButton(): void {
    $("#registerButton").prop("disabled", false).html('<span class="fa fa-user-plus"></span> Register');
}

function getResponseValue(result: any, pascalName: string, camelName: string): any {
    if (!result) {
        return undefined;
    }
    if (typeof result[pascalName] !== "undefined") {
        return result[pascalName];
    }
    return result[camelName];
}

function getErrorCode(result: any): string {
    return getResponseValue(result, "ErrorCode", "errorCode") || "";
}

function validatePassword(password: string): string {
    if (password.length < 8) {
        return "Password must be at least 8 characters long";
    }
    if (!/[A-Z]/.test(password)) {
        return "Password must contain at least one uppercase letter";
    }
    if (!/[a-z]/.test(password)) {
        return "Password must contain at least one lowercase letter";
    }
    if (!/[0-9]/.test(password)) {
        return "Password must contain at least one digit";
    }
    return "";
}

function handleLogin(event: Event): void {
    event.preventDefault();

    if (!checkRateLimit()) {
        return;
    }

    $("#loginError").hide();
    const email = ($("#email").val() as string).trim();
    const password = ($("#password").val() as string);

    if (!email || !password) {
        showError("loginError", "loginErrorMessage", "Email and password are required");
        return;
    }

    $("#loginButton").prop("disabled", true).html('<span class="fa fa-spinner fa-spin"></span> Logging in...');

    postJson("/webapi/auth/login", {
        Email: email,
        Password: password
    }).then((result: AuthResponse) => {
        const isSuccess = getResponseValue(result, "Success", "success");
        if (isSuccess === true) {
            showSuccess("loginSuccess");
            loginAttempts = 0;
            setTimeout(() => {
                window.location.href = "/index.html";
            }, 1500);
            return;
        }
        if (getErrorCode(result) === "InvalidCredentials") {
            showError("loginError", "loginErrorMessage", getResponseValue(result, "ErrorMessage", "errorMessage") || "Invalid email or password");
            resetLoginButton();
            return;
        }
        if (getErrorCode(result) === "AccountLocked") {
            const lockedUntilValue = getResponseValue(result, "LockedUntil", "lockedUntil");
            const lockedUntil = new Date(lockedUntilValue);
            const timeLeft = Math.ceil((lockedUntil.getTime() - Date.now()) / 60000);
            showError("loginError", "loginErrorMessage", `Account is locked due to too many failed attempts. Try again in ${timeLeft} minutes.`);
            resetLoginButton();
            return;
        }
        if (getErrorCode(result) === "AccountInactive") {
            showError("loginError", "loginErrorMessage", getResponseValue(result, "ErrorMessage", "errorMessage") || "Account is inactive. Please contact support.");
            resetLoginButton();
            return;
        }
        showError("loginError", "loginErrorMessage", "Unexpected login response. Please try again.");
        resetLoginButton();
    }).catch((error: any) => {
        console.log("Login error:", error);
        showError("loginError", "loginErrorMessage", "An error occurred during login. Please try again.");
        resetLoginButton();
    });
}

function handleRegister(event: Event): void {
    event.preventDefault();

    $("#registerError").hide();
    const email = ($("#regEmail").val() as string).trim();
    const password = ($("#regPassword").val() as string);

    if (!email || !password) {
        showError("registerError", "registerErrorMessage", "All fields are required");
        return;
    }

    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(email)) {
        showError("registerError", "registerErrorMessage", "Please enter a valid email address");
        return;
    }

    const passwordValidationError = validatePassword(password);
    if (passwordValidationError) {
        showError("registerError", "registerErrorMessage", passwordValidationError);
        return;
    }

    $("#registerButton").prop("disabled", true).html('<span class="fa fa-spinner fa-spin"></span> Registering...');

    postJson("/webapi/auth/register", {
        Email: email,
        Password: password
    }).then((result: AuthResponse) => {
        const isSuccess = getResponseValue(result, "Success", "success");
        if (isSuccess === true) {
            $('#registerForm').trigger("reset");
            resetRegisterButton();
            activateTab("login");
            showTransientSuccess("registerSuccess", 3000);
            return;
        }
        if (getErrorCode(result) === "EmailExists") {
            showError("registerError", "registerErrorMessage", getResponseValue(result, "ErrorMessage", "errorMessage") || "Email already registered. Please use another email.");
            resetRegisterButton();
            return;
        }
        if (getErrorCode(result) === "WeakPassword") {
            showError("registerError", "registerErrorMessage", getResponseValue(result, "ErrorMessage", "errorMessage") || "Password is too weak");
            resetRegisterButton();
            return;
        }
        showError("registerError", "registerErrorMessage", "Unexpected registration response. Please try again.");
        resetRegisterButton();
    }).catch((error: any) => {
        console.log("Registration error:", error);
        showError("registerError", "registerErrorMessage", "An error occurred during registration. Please try again.");
        resetRegisterButton();
    });
}

export function initLoginPage(locale: string): void {
    console.log("Initializing login page with locale:", locale);
    $("#loginForm").on("submit", handleLogin);
    $("#registerForm").on("submit", handleRegister);
    $(".login-tab-button").on("click", function() {
        const tabName = $(this).attr("data-auth-tab");
        activateTab(tabName || "login");
    });

    $("#email").focus();

    $("#loginError").hide();
    $("#loginSuccess").hide();
    $("#registerError").hide();
    $("#registerSuccess").hide();
}

export function logout(): Promise<void> {
    return postJson("/webapi/auth/logout", {}).then(() => {
        window.location.href = "/login.html";
    });
}
