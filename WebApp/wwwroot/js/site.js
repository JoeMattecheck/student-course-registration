// Automatically dismiss success messages after 5 seconds
document.addEventListener("DOMContentLoaded", function () {
    const alerts = document.querySelectorAll(".alert-success");

    alerts.forEach(function (alert) {
        setTimeout(function () {
            const bsAlert = bootstrap.Alert.getOrCreateInstance(alert);
            bsAlert.close();
        }, 5000);
    });
});


// Reusable live search for all search bars
function performSearch(searchInput, itemSelector, groupSelector = null) {

    let filter = searchInput.value.toLowerCase();

    let items = document.querySelectorAll(itemSelector);

    items.forEach(item => {
        let text = item.textContent.toLowerCase();

        item.style.display = text.includes(filter) ? "" : "none";
    });

    // Hide empty groups if grouping is used
    if (groupSelector) {
        let groups = document.querySelectorAll(groupSelector);

        groups.forEach(group => {
            let visibleItems = group.querySelectorAll(
                itemSelector + ':not([style*="display: none"])'
            );

            group.style.display =
                visibleItems.length > 0 ? "" : "none";
        });
    }
}

// Automatically connect all search bars
document.addEventListener("DOMContentLoaded", function () {

    // Administrator course search
    let courseSearch = document.getElementById("courseSearch");

    if (courseSearch) {
        courseSearch.addEventListener("input", function () {
            performSearch(courseSearch, ".course-item");
        });
    }

    // Administrator student search
    let studentSearch = document.getElementById("studentSearch");

    if (studentSearch) {
        studentSearch.addEventListener("input", function () {
            performSearch(studentSearch, ".student-item");
        });
    }

    // Student available course search
    let availableCourseSearch = document.getElementById(
        "availableCourseSearch"
    );

    if (availableCourseSearch) {
        availableCourseSearch.addEventListener("input", function () {
            performSearch(
                availableCourseSearch,
                ".available-course-item",
                ".semester-group"
            );
        });
    }
});
function togglePassword(inputId, button) {
    const password = document.getElementById(inputId);

    if (password.type === "password") {
        password.type = "text";
        button.textContent = "Hide Password";
    } else {
        password.type = "password";
        button.textContent = "Show Password";
    }
}
