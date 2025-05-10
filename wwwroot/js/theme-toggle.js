document.addEventListener("DOMContentLoaded", function () {
    const themeToggle = document.getElementById("themeToggle");
    const body = document.body;

    themeToggle.addEventListener("click", function () {
        const currentTheme = body.getAttribute("data-theme");
        const newTheme = currentTheme === "dark" ? "light" : "dark";

        // Update the theme on the body
        body.setAttribute("data-theme", newTheme);

        // Update the icon
        themeToggle.innerHTML = newTheme === "dark" ? '<i class="bi bi-sun"></i>' : '<i class="bi bi-moon"></i>';

        // Persist the preference via AJAX
        fetch("/Shared/SetTheme", {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "X-CSRF-TOKEN": document.querySelector('input[name="__RequestVerificationToken"]').value
            },
            body: JSON.stringify({ theme: newTheme })
        }).catch((error) => console.error("Error updating theme preference:", error));
    });
});
