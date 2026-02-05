const screen = document.getElementById("screen");
const inputLine = document.getElementById("inputLine");

let buffer = "";

const bootLines = [
    "**** COMMODORE 64 BASIC V2 ****",
    "64K RAM SYSTEM  38911 BASIC BYTES FREE",
    "",
    "READY."
];

const terminal = document.getElementById("terminal");
const hiddenInput = document.getElementById("hiddenInput");

terminal.addEventListener("click", () => {
    hiddenInput.focus();
});

async function sleep(ms) {
    return new Promise(r => setTimeout(r, ms));
}

async function typeLine(text) {
    const div = document.createElement("div");

    if (text === "") {
        div.innerHTML = "&nbsp;";
        screen.appendChild(div);
        screen.parentElement.scrollTop = screen.parentElement.scrollHeight;
        return;
    }

    screen.appendChild(div);

    for (const ch of text) {
        div.textContent += ch;
        screen.parentElement.scrollTop = screen.parentElement.scrollHeight;
        await sleep(18);
    }
}

async function boot() {
    for (const l of bootLines) {
        await typeLine(l);
    }
}

async function handleCommand(cmd) {
    await typeLine("> " + cmd);

    if (!cmd) {
        await typeLine("READY.");
        return;
    }

    if (cmd === "NEW") {
        await fetch("/api/reset", { method: "POST" });
        await typeLine("READY.");
        return;
    }

    const res = await fetch("/api/chat", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ prompt: cmd })
    });

    if (!res.ok) {
        await typeLine("?SYNTAX ERROR");
        await typeLine("READY.");
        return;
    }

    const data = await res.json();
    for (const line of data.reply.split("\n")) {
        await typeLine(line.toUpperCase());
    }
}

// Handle desktop keyboard
document.addEventListener("keydown", async e => {
    // Only handle if input is NOT focused (desktop)
    if (document.activeElement !== hiddenInput) {
        handleKey(e.key);
        e.preventDefault(); // prevent scrolling on space/backspace
    }
});

// Handle mobile/hidden input
hiddenInput.addEventListener("input", e => {
    const char = e.target.value;
    if (char) {
        handleKey(char);
        e.target.value = ""; // clear input
    }
});

hiddenInput.addEventListener("keydown", e => {
    if (e.key === "Enter") {
        handleKey("Enter");
        e.preventDefault(); // prevent default newline
    } else if (e.key === "Backspace") {
        handleKey("Backspace");
        e.preventDefault();
    }
});

function handleKey(key) {
    if (key === "Enter") {
        const cmd = buffer;
        buffer = "";
        inputLine.textContent = "";
        handleCommand(cmd);
        return;
    } else if (key === "Backspace") {
        buffer = buffer.slice(0, -1);
    } else if (key.length === 1) {
        buffer += key.toUpperCase();
    }

    inputLine.textContent = buffer;
}

boot();
