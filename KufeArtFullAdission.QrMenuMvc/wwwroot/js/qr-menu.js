// ===== QR MENU ENGINE =====
class QRMenuEngine {
    constructor() {
        this.products = [];
        this.categories = [];
        this.currentCategory = 'all';
        this.weatherData = null;
        this.suggestionShown = false;

        this.init();
    }

    async init() {
        console.log('🚀 QR Menu initializing...');

        // Start loading animation
        this.showLoader();

        try {
            // Parallel loading for better performance
            await Promise.all([
                this.loadProducts(),
                this.loadWeatherData(),
                this.trackPageView()
            ]);

            // Initialize UI components
            this.initializeUI();
            this.startClock();

            // Hide loader and show app
            setTimeout(() => {
                this.hideLoader();
                this.scheduleWeatherSuggestion();
            }, 2000); // Minimum 2 seconds loading for better UX

        } catch (error) {
            console.error('❌ Initialization error:', error);
            this.showError('Menü yüklenirken hata oluştu. Lütfen sayfayı yenileyin.');
        }
    }

    showLoader() {
        document.getElementById('globalLoader').style.display = 'flex';
        document.getElementById('qrMenuApp').style.display = 'none';
    }

    hideLoader() {
        document.getElementById('globalLoader').style.display = 'none';
        document.getElementById('qrMenuApp').style.display = 'block';

        // Smooth entrance animation
        document.getElementById('qrMenuApp').style.opacity = '0';
        setTimeout(() => {
            document.getElementById('qrMenuApp').style.opacity = '1';
            document.getElementById('qrMenuApp').style.transition = 'opacity 0.5s ease';
        }, 100);
    }

    async loadProducts() {
        try {
            console.log('📦 Loading products...');
            const response = await fetch('/api/menu/products');
            if (!response.ok) throw new Error('Products load failed');

            const data = await response.json();
            this.products = data.products || [];
            this.categories = data.categories || [];

            console.log(`✅ Loaded ${this.products.length} products in ${this.categories.length} categories`);
        } catch (error) {
            console.error('❌ Products loading error:', error);
            throw error;
        }
    }

    async loadWeatherData() {
        try {
            console.log('🌤️ Loading weather data...');
            const response = await fetch('/api/weather/current');
            if (!response.ok) throw new Error('Weather load failed');

            this.weatherData = await response.json();
            console.log('✅ Weather data loaded:', this.weatherData);
        } catch (error) {
            console.error('❌ Weather loading error:', error);
            // Weather is not critical, continue without it
            this.weatherData = null;
        }
    }

    async trackPageView() {
        try {
            await fetch('/api/menu/track-view', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ timestamp: new Date().toISOString() })
            });
            console.log('📊 Page view tracked');
        } catch (error) {
            console.error('❌ Tracking error:', error);
            // Tracking is not critical
        }
    }

    initializeUI() {
        this.renderCategories();
        this.renderProducts();
        this.updateWeatherDisplay();
    }

    renderCategories() {
        const container = document.getElementById('categoryTabs');

        if (!this.categories.length) {
            container.innerHTML = '<div class="text-center py-3">Kategori bulunamadı</div>';
            return;
        }

        // Add "All" category
        const allCategory = {
            name: 'all',
            displayName: 'Tümü',
            icon: 'fas fa-th-large',
            productCount: this.products.length
        };

        const allCategories = [allCategory, ...this.categories];

        const categoriesHTML = allCategories.map(category => `
            <div class="category-tab ${category.name === this.currentCategory ? 'active' : ''}" 
                 data-category="${category.name}"
                 onclick="QRMenu.selectCategory('${category.name}')">
                <i class="${category.icon || 'fas fa-tag'}"></i>
                ${category.displayName || category.name}
                <span class="category-badge">${category.productCount}</span>
            </div>
        `).join('');

        container.innerHTML = categoriesHTML;
    }

    selectCategory(categoryName) {
        this.currentCategory = categoryName;

        // Update active tab
        document.querySelectorAll('.category-tab').forEach(tab => {
            tab.classList.remove('active');
        });
        document.querySelector(`[data-category="${categoryName}"]`).classList.add('active');

        // Re-render products
        this.renderProducts();

        // Smooth scroll to top of products
        document.querySelector('.products-container').scrollIntoView({
            behavior: 'smooth'
        });
    }

    renderProducts() {
        const container = document.getElementById('productsGrid');

        let filteredProducts = this.products;

        // Filter by category
        if (this.currentCategory !== 'all') {
            filteredProducts = this.products.filter(product =>
                product.categoryName.toLowerCase().replace(/\s+/g, '') === this.currentCategory.toLowerCase()
            );
        }

        if (!filteredProducts.length) {
            container.innerHTML = `
                <div class="col-12 text-center py-5">
                    <i class="fas fa-search fa-3x text-muted mb-3 opacity-25"></i>
                    <h6 class="text-muted">Bu kategoride ürün bulunamadı</h6>
                </div>
            `;
            return;
        }

        const productsHTML = filteredProducts.map(product => this.createProductCard(product)).join('');
        container.innerHTML = productsHTML;

        // Initialize image carousels
        this.initializeImageCarousels();
    }

    createProductCard(product) {
        // ✅ GÜNCEL: Resim yoksa boş string kullan, CSS halleder
        const images = product.images && product.images.length > 0 ? product.images : [''];
        const firstImage = images[0] || '';

        return `
        <div class="product-card" data-product-id="${product.id}">
            <div class="product-image-container">
                <img src="${firstImage}" alt="${product.name}" class="product-image">
                
                ${product.hasCampaign ? `
                    <div class="campaign-badge">
                        ${product.campaignCaption || '🎯 Kampanya'}
                    </div>
                ` : ''}
                
                ${images.length > 1 && images[0] !== '' ? `
                    <div class="image-indicators">
                        ${images.map((_, index) => `
                            <div class="image-dot ${index === 0 ? 'active' : ''}" 
                                 data-image-index="${index}"></div>
                        `).join('')}
                    </div>
                ` : ''}
            </div>
            
            <div class="product-info">
                <h3 class="product-name">${product.name}</h3>
                ${product.description ? `
                    <p class="product-description">${product.description}</p>
                ` : ''}
                <div class="product-price">₺${product.price.toFixed(2)}</div>
            </div>
        </div>
    `;
    }

    initializeImageCarousels() {
        // Add click handlers for image indicators
        document.querySelectorAll('.image-dot').forEach(dot => {
            dot.addEventListener('click', (e) => {
                e.stopPropagation();
                const imageIndex = parseInt(e.target.dataset.imageIndex);
                const productCard = e.target.closest('.product-card');
                const productId = productCard.dataset.productId;
                this.changeProductImage(productId, imageIndex);
            });
        });

        // Add swipe support for mobile
        this.initializeSwipeSupport();
    }

    changeProductImage(productId, imageIndex) {
        const product = this.products.find(p => p.id === productId);
        // ✅ GÜNCEL: Resim kontrolü
        if (!product || !product.images || imageIndex >= product.images.length || product.images[0] === '') return;

        const productCard = document.querySelector(`[data-product-id="${productId}"]`);
        const imageElement = productCard.querySelector('.product-image');
        const dots = productCard.querySelectorAll('.image-dot');

        // Update image
        imageElement.src = product.images[imageIndex] || '';

        // Update active dot
        dots.forEach(dot => dot.classList.remove('active'));
        if (dots[imageIndex]) {
            dots[imageIndex].classList.add('active');
        }
    }

    initializeSwipeSupport() {
        // Touch/swipe support for product images
        let startX = 0;
        let currentProductId = null;

        document.querySelectorAll('.product-image-container').forEach(container => {
            container.addEventListener('touchstart', (e) => {
                startX = e.touches[0].clientX;
                currentProductId = e.target.closest('.product-card').dataset.productId;
            });

            container.addEventListener('touchend', (e) => {
                if (!currentProductId) return;

                const endX = e.changedTouches[0].clientX;
                const diffX = startX - endX;

                if (Math.abs(diffX) > 50) { // Minimum swipe distance
                    const product = this.products.find(p => p.id === currentProductId);
                    // ✅ GÜNCEL: Resim yoksa veya tek resimse swipe'ı devre dışı bırak
                    if (!product || !product.images || product.images.length <= 1 || product.images[0] === '') return;

                    const currentDot = document.querySelector(`[data-product-id="${currentProductId}"] .image-dot.active`);
                    if (!currentDot) return; // Dot yoksa çık

                    const currentIndex = parseInt(currentDot.dataset.imageIndex);

                    let newIndex;
                    if (diffX > 0) { // Swipe left - next image
                        newIndex = (currentIndex + 1) % product.images.length;
                    } else { // Swipe right - previous image
                        newIndex = currentIndex === 0 ? product.images.length - 1 : currentIndex - 1;
                    }

                    this.changeProductImage(currentProductId, newIndex);
                }

                currentProductId = null;
            });
        });
    }

    updateWeatherDisplay() {
        const weatherMini = document.getElementById('weatherMini');

        if (!this.weatherData || !this.weatherData.success) {
            weatherMini.innerHTML = '<i class="fas fa-cloud"></i> --°';
            return;
        }

        const temp = Math.round(this.weatherData.temperature);
        const icon = this.getWeatherIcon(this.weatherData.condition);

        weatherMini.innerHTML = `<i class="${icon}"></i> ${temp}°`;
    }

    getWeatherIcon(condition) {
        const iconMap = {
            'clear': 'fas fa-sun',
            'clouds': 'fas fa-cloud',
            'rain': 'fas fa-cloud-rain',
            'snow': 'fas fa-snowflake',
            'thunderstorm': 'fas fa-bolt',
            'drizzle': 'fas fa-cloud-drizzle',
            'mist': 'fas fa-smog',
            'fog': 'fas fa-smog'
        };

        return iconMap[condition?.toLowerCase()] || 'fas fa-cloud';
    }

    startClock() {
        const updateTime = () => {
            const now = new Date();
            const timeString = now.toLocaleTimeString('tr-TR', {
                hour: '2-digit',
                minute: '2-digit'
            });
            document.getElementById('currentTime').textContent = timeString;
        };

        updateTime();
        setInterval(updateTime, 1000);
    }

    scheduleWeatherSuggestion() {
        // Show suggestion after 7 seconds
        setTimeout(() => {
            if (!this.suggestionShown && this.weatherData && this.weatherData.success) {
                this.showWeatherSuggestion();
            }
        }, 7000);
    }

    showWeatherSuggestion() {
        if (this.suggestionShown) return;

        const suggestion = this.generateWeatherSuggestion();
        if (!suggestion) return;

        const popup = document.getElementById('weatherSuggestion');
        const body = popup.querySelector('.suggestion-body');

        body.innerHTML = `
            <div class="weather-icon">${suggestion.icon}</div>
            <div class="suggestion-message">${suggestion.message}</div>
            <div class="recommended-products">
                ${suggestion.products.map(product => `
                    <span class="recommended-product">${product}</span>
                `).join('')}
            </div>
        `;

        popup.style.display = 'flex';
        this.suggestionShown = true;

        // Auto close after 10 seconds
        setTimeout(() => {
            this.closeSuggestion();
        }, 10000);
    }

    generateWeatherSuggestion() {
        if (!this.weatherData || !this.weatherData.success) return null;

        const temp = this.weatherData.temperature;
        const condition = this.weatherData.condition?.toLowerCase();
        const hour = new Date().getHours();

        // Temperature-based suggestions
        if (temp < 10) {
            return {
                icon: '🥶',
                message: 'Brr! Dışarısı çok soğuk. Sıcacık içeceklerle ısınmaya ne dersin?',
                products: ['Sıcak Çikolata', 'Türk Kahvesi', 'Bitki Çayı', 'Cappuccino']
            };
        }

        if (temp > 25) {
            return {
                icon: '☀️',
                message: 'Güneşli ve sıcak bir gün! Serinletici lezzetler seni bekliyor.',
                products: ['Soğuk Kahve', 'Buzlu Çay', 'Limonata', 'Milkshake']
            };
        }

        // Weather condition based
        if (condition?.includes('rain')) {
            return {
                icon: '🌧️',
                message: 'Yağmurlu günlerde sıcak içecekler çok keyifli oluyor!',
                products: ['Cappuccino', 'Latte', 'Sıcak Çikolata', 'Earl Grey']
            };
        }

        if (condition?.includes('cloud')) {
            return {
                icon: '☁️',
                message: 'Bulutlu havalarda kahve molası tam zamanı!',
                products: ['Americano', 'Flat White', 'Mocha', 'Cheesecake']
            };
        }

        // Time-based fallback
        if (hour >= 6 && hour < 11) {
            return {
                icon: '🌅',
                message: 'Günaydın! Güne enerjik başlamak için neler var bakalım?',
                products: ['Americano', 'Croissant', 'Omlet', 'Fresh Orange']
            };
        }

        if (hour >= 14 && hour < 17) {
            return {
                icon: '☕',
                message: 'Öğleden sonra enerjini toplamak için tatlı bir mola?',
                products: ['Latte', 'Cheesecake', 'Brownie', 'Cappuccino']
            };
        }

        return null;
    }

    closeSuggestion() {
        const popup = document.getElementById('weatherSuggestion');
        popup.style.display = 'none';
    }

    showError(message) {
        this.hideLoader();
        const container = document.getElementById('productsGrid');
        container.innerHTML = `
            <div class="col-12 text-center py-5">
                <i class="fas fa-exclamation-triangle fa-3x text-danger mb-3"></i>
                <h5 class="text-danger">${message}</h5>
                <button class="btn btn-primary mt-3" onclick="location.reload()">
                    <i class="fas fa-refresh me-2"></i>Sayfayı Yenile
                </button>
            </div>
        `;
    }
}

// ===== GLOBAL INSTANCE =====
let QRMenu;

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    QRMenu = new QRMenuEngine();
});

// Global functions for HTML onclick handlers
window.QRMenu = {
    selectCategory: (category) => QRMenu?.selectCategory(category),
    closeSuggestion: () => QRMenu?.closeSuggestion()
};