/* ========================================================
   HBOperations Presentation — Interactive Script
   ======================================================== */

(function () {
    'use strict';

    const slides = document.querySelectorAll('.slide');
    const navLinks = document.querySelectorAll('.nav-link');
    const progressBar = document.getElementById('progressBar');
    const currentSlideEl = document.getElementById('currentSlide');
    const totalSlidesEl = document.getElementById('totalSlides');
    const keyboardHint = document.getElementById('keyboardHint');
    const navBar = document.getElementById('navBar');

    let currentIndex = 0;
    let isTransitioning = false;
    const totalSlides = slides.length;

    totalSlidesEl.textContent = totalSlides;

    // =================== NAVIGATION ===================
    function goToSlide(index, direction) {
        if (isTransitioning || index === currentIndex || index < 0 || index >= totalSlides) return;
        isTransitioning = true;

        const prev = slides[currentIndex];
        const next = slides[index];

        // Exit animation
        prev.classList.remove('active');
        prev.classList.add('exit-left');
        setTimeout(() => prev.classList.remove('exit-left'), 700);

        // Enter animation
        next.classList.add('active');

        // Update nav
        navLinks.forEach(l => l.classList.remove('active'));
        navLinks[index]?.classList.add('active');

        // Scroll nav link into view
        navLinks[index]?.scrollIntoView({ behavior: 'smooth', block: 'nearest', inline: 'center' });

        // Update counter
        currentIndex = index;
        currentSlideEl.textContent = currentIndex + 1;
        progressBar.style.width = ((currentIndex) / (totalSlides - 1) * 100) + '%';

        // Hide hint after first navigation
        if (keyboardHint) keyboardHint.style.opacity = '0';

        setTimeout(() => {
            isTransitioning = false;
            // Trigger counter animations on dashboard slide
            if (index === 4) animateCounters();
        }, 650);
    }

    function nextSlide() { goToSlide(currentIndex + 1, 'next'); }
    function prevSlide() { goToSlide(currentIndex - 1, 'prev'); }

    // =================== KEYBOARD ===================
    document.addEventListener('keydown', function (e) {
        switch (e.key) {
            case 'ArrowLeft':
            case 'ArrowDown':
            case ' ':
            case 'PageDown':
                e.preventDefault();
                nextSlide();
                break;
            case 'ArrowRight':
            case 'ArrowUp':
            case 'PageUp':
                e.preventDefault();
                prevSlide();
                break;
            case 'Home':
                e.preventDefault();
                goToSlide(0);
                break;
            case 'End':
                e.preventDefault();
                goToSlide(totalSlides - 1);
                break;
            case 'f':
            case 'F':
                e.preventDefault();
                toggleFullscreen();
                break;
        }
    });

    // =================== CLICK NAV ===================
    navLinks.forEach(link => {
        link.addEventListener('click', function (e) {
            e.preventDefault();
            const slideIndex = parseInt(this.dataset.slide);
            goToSlide(slideIndex);
        });
    });

    // Cover scroll hint click
    const scrollHint = document.querySelector('.cover-scroll-hint');
    if (scrollHint) {
        scrollHint.addEventListener('click', nextSlide);
    }

    // =================== TOUCH / SWIPE ===================
    let touchStartX = 0;
    let touchStartY = 0;

    document.addEventListener('touchstart', function (e) {
        touchStartX = e.changedTouches[0].screenX;
        touchStartY = e.changedTouches[0].screenY;
    }, { passive: true });

    document.addEventListener('touchend', function (e) {
        const dx = e.changedTouches[0].screenX - touchStartX;
        const dy = e.changedTouches[0].screenY - touchStartY;

        // Only respond to horizontal swipes (|dx| > |dy| and |dx| > 50px)
        if (Math.abs(dx) > Math.abs(dy) && Math.abs(dx) > 50) {
            if (dx > 0) {
                // Swipe right → next (RTL)
                nextSlide();
            } else {
                // Swipe left → prev (RTL)
                prevSlide();
            }
        }
    }, { passive: true });

    // =================== MOUSE WHEEL ===================
    let wheelCooldown = false;
    document.addEventListener('wheel', function (e) {
        if (wheelCooldown) return;
        wheelCooldown = true;

        if (e.deltaY > 30) {
            nextSlide();
        } else if (e.deltaY < -30) {
            prevSlide();
        }

        setTimeout(() => { wheelCooldown = false; }, 800);
    }, { passive: true });

    // =================== FULLSCREEN ===================
    function toggleFullscreen() {
        if (!document.fullscreenElement) {
            document.documentElement.requestFullscreen?.();
        } else {
            document.exitFullscreen?.();
        }
    }

    // =================== COUNTER ANIMATION ===================
    function animateCounters() {
        const counters = document.querySelectorAll('.dash-stat-value[data-count]');
        counters.forEach(counter => {
            const target = counter.dataset.count.replace(/,/g, '');
            const num = parseInt(target);
            const duration = 1200;
            const start = Date.now();

            function update() {
                const elapsed = Date.now() - start;
                const progress = Math.min(elapsed / duration, 1);
                // Ease out cubic
                const eased = 1 - Math.pow(1 - progress, 3);
                const current = Math.round(num * eased);
                counter.textContent = current.toLocaleString('en-US');
                if (progress < 1) requestAnimationFrame(update);
            }
            requestAnimationFrame(update);
        });
    }

    // =================== AUTO-HIDE NAV ===================
    let navTimeout;
    function showNav() {
        navBar.classList.remove('hidden');
        clearTimeout(navTimeout);
        // Only auto-hide on cover slide
        if (currentIndex === 0) {
            navTimeout = setTimeout(() => navBar.classList.add('hidden'), 3000);
        }
    }

    document.addEventListener('mousemove', function (e) {
        if (e.clientY < 80) showNav();
    });

    // =================== INIT ===================
    progressBar.style.width = '0%';
    currentSlideEl.textContent = '1';

    // Hide keyboard hint after 5 seconds
    setTimeout(() => {
        if (keyboardHint) keyboardHint.style.opacity = '0';
    }, 6000);

})();
