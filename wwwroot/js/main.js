/* ========================================= */
/* MOBILE MENU */
/* ========================================= */

const hamburger = document.getElementById("hamburger");

if (hamburger) {
    hamburger.addEventListener("click", () => {
        document
            .querySelector(".nav-links")
            .classList.toggle("active");
    });
}

/* ========================================= */
/* TYPING ANIMATION */
/* ========================================= */

const typingElement = document.getElementById("typing-text");

if (typingElement) {

    const words = [
        "Learn at Your Own Pace",
        "Master New Skills",
        "Build Your Future with APU"
    ];

    let wordIndex = 0;
    let charIndex = 0;
    let deleting = false;

    function typeEffect() {

        const currentWord = words[wordIndex];

        if (!deleting) {

            typingElement.textContent =
                currentWord.substring(0, charIndex + 1);

            charIndex++;

            if (charIndex === currentWord.length) {

                deleting = true;

                setTimeout(typeEffect, 1500);

                return;
            }

        } else {

            typingElement.textContent =
                currentWord.substring(0, charIndex - 1);

            charIndex--;

            if (charIndex === 0) {

                deleting = false;

                wordIndex++;

                if (wordIndex >= words.length) {
                    wordIndex = 0;
                }
            }
        }

        setTimeout(typeEffect, deleting ? 50 : 100);
    }

    typeEffect();
}

/* ========================================= */
/* STICKY NAVBAR */
/* ========================================= */

window.addEventListener("scroll", () => {

    const navbar = document.querySelector(".navbar");

    if (!navbar) return;

    if (window.scrollY > 60) {

        navbar.classList.add("navbar-scrolled");

    } else {

        navbar.classList.remove("navbar-scrolled");
    }
});

/* ========================================= */
/* SCROLL REVEAL */
/* ========================================= */

const reveals = document.querySelectorAll(".reveal");

const observer = new IntersectionObserver(

(entries) => {

    entries.forEach((entry) => {

        if (entry.isIntersecting) {

            entry.target.classList.add("active-reveal");
        }
    });

},
{
    threshold: 0.15
}

);

reveals.forEach((item) => {
    observer.observe(item);
});

/* ========================================= */
/* COUNTER ANIMATION */
/* ========================================= */

const counters = document.querySelectorAll(".counter");

const counterObserver = new IntersectionObserver(

(entries) => {

entries.forEach((entry) => {

if (entry.isIntersecting) {

const counter = entry.target;

const target =
parseInt(counter.getAttribute("data-target"));

let count = 0;

const speed = target / 80;

const updateCounter = () => {

count += speed;

if (count < target) {

counter.innerText = Math.floor(count);

requestAnimationFrame(updateCounter);

} else {

counter.innerText = target;
}

};

updateCounter();

counterObserver.unobserve(counter);

}

});

},
{
threshold: 0.5
}

);

counters.forEach(counter => {
counterObserver.observe(counter);
});

/* ========================================= */
/* COURSE FILTER */
/* ========================================= */

const filterButtons =
document.querySelectorAll(".filter-btn");

const courseCards =
document.querySelectorAll(".course-card");

filterButtons.forEach(button => {

button.addEventListener("click", () => {

document
.querySelector(".filter-btn.active")
?.classList.remove("active");

button.classList.add("active");

const filter =
button.getAttribute("data-filter");

courseCards.forEach(card => {

const category =
card.getAttribute("data-category");

if (
filter === "all" ||
category === filter
) {

card.style.display = "block";

} else {

card.style.display = "none";
}

});

});

});

/* ========================================= */
/* DARK MODE */
/* ========================================= */

const themeToggle =
document.getElementById("themeToggle");

if (themeToggle) {

const savedTheme =
localStorage.getItem("theme");

if (savedTheme === "light") {

document.body.classList.add("light-mode");

themeToggle.innerHTML = "☀️";

}

themeToggle.addEventListener("click", () => {

document.body.classList.toggle("light-mode");

if (
document.body.classList.contains("light-mode")
) {

localStorage.setItem("theme", "light");

themeToggle.innerHTML = "☀️";

} else {

localStorage.setItem("theme", "dark");

themeToggle.innerHTML = "🌙";

}

});

}

/* ========================================= */
/* VIEW TRANSITIONS */
/* ========================================= */

document
.querySelectorAll("a")
.forEach(link => {

link.addEventListener("click", (e) => {

const href = link.getAttribute("href");

if (
href &&
href.endsWith(".html") &&
document.startViewTransition
) {

e.preventDefault();

document.startViewTransition(() => {
window.location.href = href;
});

}

});

});
/* ========================================= */
/* STUDENT DASHBOARD SECTION SWITCH */
/* ========================================= */


function showSection(sectionId)
{


const sections =
document.querySelectorAll(".content-section");



sections.forEach(section=>{

section.style.display="none";

});



const selected =
document.getElementById(sectionId);



if(selected)
{

selected.style.display="block";

}





const menuItems =
document.querySelectorAll(".menu a");



menuItems.forEach(item=>{

item.classList.remove("active");

});



event.target.classList.add("active");


}