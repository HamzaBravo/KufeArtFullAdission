// ===== QR MENU ENGINE =====
class QRMenuEngine {
    constructor() {
        this.products = [];
        this.categories = [];
        this.currentView = 'categories'; // 'categories' veya 'products'
        this.currentCategory = null;
        this.weatherData = null;
        this.suggestionShown = false;
        this.loadingStates = {
            products: false,
            weather: false,
            tracking: false
        };

        this.init();
    }

    async init() {
        console.log('🚀 QR Menu initializing...');
        this.showLoader();

        try {
            await Promise.all([
                this.loadProducts(),
                this.loadWeatherData(),
                this.trackPageView()
            ]);

            this.initializeUI();
            this.hideLoader();
            this.scheduleWeatherSuggestion();

        } catch (error) {
            console.error('❌ Initialization error:', error);
            this.showError('Menü yüklenirken hata oluştu. Lütfen sayfayı yenileyin.');
        }
    }

    showLoader() {
        const loader = document.getElementById('globalLoader');
        const app = document.getElementById('qrMenuApp');

        loader.style.display = 'flex';
        app.style.display = 'none';
    }

    hideLoader() {
        const allLoaded = Object.values(this.loadingStates).every(state => state);

        if (!allLoaded) {
            console.log('⏳ Waiting for all components to load...');
            return;
        }

        const loader = document.getElementById('globalLoader');
        const app = document.getElementById('qrMenuApp');

        loader.style.opacity = '0';
        setTimeout(() => {
            loader.style.display = 'none';
            app.style.display = 'block';
            app.style.opacity = '0';

            setTimeout(() => {
                app.style.opacity = '1';
                app.style.transition = 'opacity 0.5s ease';
            }, 100);
        }, 500);
    }

    async loadProducts() {
        try {
            console.log('📦 Loading products...');
            const response = await fetch('/api/menu/products');
            if (!response.ok) throw new Error('Products load failed');

            const data = await response.json();
            this.products = data.products || [];
            this.categories = data.categories || [];

            this.assignRandomImagesToCategories();

            console.log(`✅ Loaded ${this.products.length} products, ${this.categories.length} categories`);
            this.loadingStates.products = true;

            // İlk yüklemede sadece kategorileri göster
            this.showCategoriesView();

            this.checkAllLoaded();
        } catch (error) {
            console.error('❌ Products loading error:', error);
            throw error;
        }
    }

    assignRandomImagesToCategories() {
        this.categories = this.categories.map(category => {
            // ✅ Normalize edilmiş isimle karşılaştır
            const categoryProducts = this.products.filter(product =>
                QRMenuEngine.normalizeCategoryName(product.categoryName) ===
                QRMenuEngine.normalizeCategoryName(category.displayName)
            );

            if (categoryProducts.length > 0) {
                const productsWithImages = categoryProducts.filter(product =>
                    product.images && product.images.length > 0 && product.images[0] !== ''
                );

                if (productsWithImages.length > 0) {
                    const randomProduct = productsWithImages[Math.floor(Math.random() * productsWithImages.length)];
                    category.randomImage = randomProduct.images[0];
                }
            }

            return category;
        });
    }

    async loadWeatherData() {
        try {
            console.log('🌤️ Loading weather data...');
            const response = await fetch('/api/weather/current');

            if (response.ok) {
                this.weatherData = await response.json();
                this.updateWeatherDisplay();
                console.log('✅ Weather data loaded');
            }

            this.loadingStates.weather = true;
            this.checkAllLoaded();
        } catch (error) {
            console.log('⚠️ Weather loading failed:', error);
            this.loadingStates.weather = true;
            this.checkAllLoaded();
        }
    }

    async trackPageView() {
        try {
            await fetch('/api/menu/track-view', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ timestamp: new Date().toISOString() })
            });
            console.log('✅ Page view tracked');
        } catch (error) {
            console.log('⚠️ Tracking failed:', error);
        } finally {
            this.loadingStates.tracking = true;
            this.checkAllLoaded();
        }
    }

    checkAllLoaded() {
        const allLoaded = Object.values(this.loadingStates).every(state => state);
        if (allLoaded) {
            console.log('🎉 All components loaded successfully');
            this.hideLoader();
        }
    }

    initializeUI() {
        this.startClock();
        this.initializeEventListeners();
        this.initializeFooter(); // ✅ Footer'ı başlat
    }

    initializeEventListeners() {
        // Kategori kartlarına click event'i
        document.addEventListener('click', (e) => {
            const categoryCard = e.target.closest('.category-card');
            if (categoryCard && this.currentView === 'categories') {
                const categoryName = categoryCard.dataset.category;
                this.selectCategory(categoryName);
            }
        });

        // Ürün kartlarına click event'i
        document.addEventListener('click', (e) => {
            const productCard = e.target.closest('.product-card');
            if (productCard && this.currentView === 'products') {
                const productId = productCard.dataset.productId;
                this.showProductModal(productId);
            }
        });

        // Modal kapatma
        document.addEventListener('click', (e) => {
            if (e.target.classList.contains('modal-close') || e.target.classList.contains('product-modal')) {
                this.closeProductModal();
            }
        });

        // ESC tuşu ile modal kapatma
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape') {
                this.closeProductModal();
            }
        });
    }

    // ===== VIEW MANAGEMENT =====
    showCategoriesView() {
        this.currentView = 'categories';
        document.body.classList.remove('show-products');
        document.body.classList.add('view-categories');

        // ✅ Header'ı kategoriler moduna çevir
        this.updateHeaderForCategories();

        this.renderCategories();
        this.clearProductsContainer();
    }

    showProductsView(categoryName) {
        this.currentView = 'products';
        this.currentCategory = categoryName;

        document.body.classList.remove('view-categories');
        document.body.classList.add('show-products');

        // ✅ Header'ı ürünler moduna çevir
        this.updateHeaderForProducts(categoryName);

        this.renderProducts();
    }

    // ✅ Header güncelleme metodlarında renk tutarlılığı
    updateHeaderForCategories() {
        const header = document.querySelector('.menu-header');
        header.classList.remove('products-mode');
        // ✅ Renk sınıflarını kaldırma, CSS'te !important ile sabitlendi

        const headerContent = header.querySelector('.row');
        headerContent.innerHTML = `
        <div class="col-8 header-content-categories">
            <h1 class="cafe-title mb-0">
                <i class="fas fa-coffee me-2"></i>
                Küfe Art
            </h1>
            <small class="cafe-subtitle">Bursa'nın kahve durağı</small>
        </div>
        <div class="col-4 text-end">
            <div class="current-time" id="currentTime"></div>
            <div class="weather-mini" id="weatherMini">
                <i class="fas fa-sun"></i> --°
            </div>
        </div>
    `;

        // Saat ve hava durumunu yeniden başlat
        this.startClock();
        this.updateWeatherDisplay();
    }

    updateHeaderForProducts(categoryName) {
        const category = this.categories.find(c => c.name === categoryName);
        if (!category) return;

        const header = document.querySelector('.menu-header');
        // ✅ products-mode class'ı ekle ama renk CSS'te sabit
        header.classList.add('products-mode');

        const headerContent = header.querySelector('.row');
        headerContent.innerHTML = `
        <div class="col-12 header-content-products">
            <div class="header-back-section">
                <button class="header-back-button" onclick="QRMenu.goBackToCategories()">
                    <i class="fas fa-arrow-left"></i>
                    Kategoriler
                </button>
                
                <div class="header-category-info">
                    <h2 class="header-category-title">
                        <i class="${category.icon || 'fas fa-utensils'} header-category-icon"></i>
                        ${category.displayName}
                    </h2>
                    <p class="header-category-subtitle">${category.productCount} ürün bulundu</p>
                </div>
                
                <div class="header-time-weather">
                    <div class="current-time" id="currentTime"></div>
                    <div class="weather-mini" id="weatherMini">
                        <i class="fas fa-sun"></i> --°
                    </div>
                </div>
            </div>
        </div>
    `;

        // Saat ve hava durumunu yeniden başlat
        this.startClock();
        this.updateWeatherDisplay();
    }

    selectCategory(categoryName) {
        console.log('🎯 Selected category:', categoryName);
        this.showProductsView(categoryName);

        // Smooth scroll to top
        window.scrollTo({ top: 0, behavior: 'smooth' });
    }

    goBackToCategories() {
        console.log('⬅️ Going back to categories');
        this.showCategoriesView();

        // Smooth scroll to top
        window.scrollTo({ top: 0, behavior: 'smooth' });
    }

    // ===== RENDER METHODS =====
    renderCategories() {
        const container = document.getElementById('mainContent');

        const categoriesHTML = this.categories.map(category => `
            <div class="category-card" data-category="${category.name}">
                <div class="category-image-container">
                    ${category.randomImage ?
                `<img src="${category.randomImage}" alt="${category.displayName}" class="category-image">` :
                `<div class="category-placeholder">
                            <i class="${category.icon || 'fas fa-utensils'}"></i>
                        </div>`
            }
                </div>
                <div class="category-name">${category.displayName || category.name}</div>
                <div class="category-count">${category.productCount} ürün</div>
            </div>
        `).join('');

        container.innerHTML = `
            <div class="view-categories">
                <div class="categories-grid">
                    ${categoriesHTML}
                </div>
            </div>
            <div class="view-products">
                <!-- Products will be rendered here -->
            </div>
        `;
    }

    renderBackNavigation() {
        const container = document.querySelector('.view-products');
        const backNavHTML = `
            <div class="back-navigation">
                <div class="container-fluid">
                    <button class="back-button" onclick="QRMenu.goBackToCategories()">
                        <i class="fas fa-arrow-left"></i>
                        Kategorilere Dön
                    </button>
                </div>
            </div>
        `;

        container.insertAdjacentHTML('afterbegin', backNavHTML);
    }

    renderCategoryHeader() {
        const category = this.categories.find(c => c.name === this.currentCategory);
        if (!category) return;

        const container = document.querySelector('.view-products');
        const headerHTML = `
            <div class="category-page-header">
                <div class="container-fluid">
                    <h2 class="category-page-title">
                        <i class="${category.icon || 'fas fa-utensils'}"></i>
                        ${category.displayName}
                    </h2>
                    <p class="category-page-subtitle">${category.productCount} ürün bulundu</p>
                </div>
            </div>
        `;

        container.insertAdjacentHTML('beforeend', headerHTML);
    }

    renderProducts() {
        // ✅ Normalize edilmiş isimle filtreleme
        let filteredProducts = this.products.filter(product =>
            product.categoryNameNormalized === this.currentCategory ||
            QRMenuEngine.normalizeCategoryName(product.categoryName) === this.currentCategory
        );

        const container = document.querySelector('.view-products');

        if (!filteredProducts.length) {
            const emptyHTML = `
                <div class="empty-state">
                    <i class="fas fa-search"></i>
                    <h5>Bu kategoride ürün bulunamadı</h5>
                    <p>Başka bir kategori deneyebilirsiniz</p>
                </div>
            `;
            container.innerHTML = emptyHTML;
            return;
        }

        const productsHTML = filteredProducts.map(product => this.createProductCard(product)).join('');

        const productsContainerHTML = `
            <main class="products-container">
                <div class="container-fluid">
                    <div class="products-grid">
                        ${productsHTML}
                    </div>
                </div>
            </main>
        `;

        container.innerHTML = productsContainerHTML;

        // ✅ Carousel'leri başlat
        this.initializeImageCarousels();
    }

    initializeImageCarousels() {
        // Otomatik döngü için timer'ları başlat
        document.querySelectorAll('.product-image-carousel').forEach(carousel => {
            const productId = carousel.dataset.productId;
            this.startAutoSlide(productId);
        });
    }

    startAutoSlide(productId) {
        // Her 4 saniyede bir resmi değiştir
        setInterval(() => {
            this.nextProductImage(productId);
        }, 2500);
    }

    // ✅ Çoklu resim destekli product card
    createProductCard(product) {
        const images = product.images && product.images.length > 0 ? product.images : [''];
        const hasMultipleImages = images.length > 1 && images[0] !== '';

        return `
            <div class="product-card" data-product-id="${product.id}">
                <div class="product-image-container">
                    ${hasMultipleImages ? `
                        <!-- Çoklu resim carousel -->
                        <div class="product-image-carousel" data-product-id="${product.id}">
                            ${images.map((image, index) => `
                                <img src="${image}" alt="${product.name}" 
                                     class="product-image ${index === 0 ? 'active' : ''}" 
                                     data-index="${index}">
                            `).join('')}
                        </div>
                        
                        <!-- Resim göstergeleri -->
                        <div class="image-indicators">
                            ${images.map((_, index) => `
                                <div class="image-dot ${index === 0 ? 'active' : ''}" 
                                     data-index="${index}" 
                                     onclick="QRMenu.changeProductImage(${product.id}, ${index})"></div>
                            `).join('')}
                        </div>
                        
                        <!-- Önceki/Sonraki butonları -->
                        <button class="carousel-btn carousel-prev" 
                                onclick="QRMenu.previousProductImage(${product.id})">
                            <i class="fas fa-chevron-left"></i>
                        </button>
                        <button class="carousel-btn carousel-next" 
                                onclick="QRMenu.nextProductImage(${product.id})">
                            <i class="fas fa-chevron-right"></i>
                        </button>
                    ` : `
                        <!-- Tek resim -->
                        <img src="${images[0]}" alt="${product.name}" class="product-image">
                    `}
                    
                    ${product.hasCampaign ? `
                        <div class="campaign-badge">
                            ${product.campaignCaption || '🎯 Kampanya'}
                        </div>
                    ` : ''}
                </div>
                <div class="product-info">
                    <div class="product-name">${product.name}</div>
                    ${product.description ? `
                        <div class="product-description">${product.description}</div>
                    ` : ''}
                    <div class="product-price">₺${product.price.toFixed(2)}</div>
                </div>
            </div>
        `;
    }

    changeProductImage(productId, targetIndex) {
        const carousel = document.querySelector(`[data-product-id="${productId}"] .product-image-carousel`);
        const dots = document.querySelectorAll(`[data-product-id="${productId}"] .image-dot`);
        const images = carousel.querySelectorAll('.product-image');

        if (!images[targetIndex]) return;

        // Aktif resmi değiştir
        images.forEach((img, index) => {
            img.classList.toggle('active', index === targetIndex);
        });

        // Aktif dot'u değiştir
        dots.forEach((dot, index) => {
            dot.classList.toggle('active', index === targetIndex);
        });
    }


    nextProductImage(productId) {
        const carousel = document.querySelector(`[data-product-id="${productId}"] .product-image-carousel`);
        if (!carousel) return;

        const currentActive = carousel.querySelector('.product-image.active');
        const currentIndex = parseInt(currentActive.dataset.index);
        const totalImages = carousel.querySelectorAll('.product-image').length;

        const nextIndex = (currentIndex + 1) % totalImages;
        this.changeProductImage(productId, nextIndex);
    }

    previousProductImage(productId) {
        const carousel = document.querySelector(`[data-product-id="${productId}"] .product-image-carousel`);
        if (!carousel) return;

        const currentActive = carousel.querySelector('.product-image.active');
        const currentIndex = parseInt(currentActive.dataset.index);
        const totalImages = carousel.querySelectorAll('.product-image').length;

        const prevIndex = currentIndex === 0 ? totalImages - 1 : currentIndex - 1;
        this.changeProductImage(productId, prevIndex);
    }

    clearProductsContainer() {
        const container = document.querySelector('.view-products');
        if (container) {
            container.innerHTML = '';
        }
    }

    // ===== MODAL METHODS =====
    // ✅ Modal'da da çoklu resim desteği
    showProductModal(productId) {
        const product = this.products.find(p => p.id == productId);
        if (!product) return;

        const images = product.images && product.images.length > 0 ? product.images : [''];
        const hasMultipleImages = images.length > 1 && images[0] !== '';

        const modalHTML = `
            <div class="product-modal" id="productModal">
                <div class="modal-content-custom">
                    <div class="modal-header-custom">
                        ${hasMultipleImages ? `
                            <!-- Modal'da da carousel -->
                            <div class="modal-image-carousel">
                                ${images.map((image, index) => `
                                    <img src="${image}" alt="${product.name}" 
                                         class="modal-image ${index === 0 ? 'active' : ''}" 
                                         data-index="${index}">
                                `).join('')}
                            </div>
                            
                            <div class="modal-image-indicators">
                                ${images.map((_, index) => `
                                    <div class="modal-image-dot ${index === 0 ? 'active' : ''}" 
                                         data-index="${index}" 
                                         onclick="QRMenu.changeModalImage(${index})"></div>
                                `).join('')}
                            </div>
                        ` : `
                            <img src="${images[0]}" alt="${product.name}" class="modal-image">
                        `}
                        
                        <button class="modal-close" onclick="QRMenu.closeProductModal()">
                            <i class="fas fa-times"></i>
                        </button>
                    </div>
                    <div class="modal-body-custom">
                        ${product.hasCampaign ? `
                            <div class="modal-campaign">
                                <i class="fas fa-fire me-2"></i>
                                ${product.campaignCaption || 'Kampanyalı Ürün!'}
                            </div>
                            ${product.campaignDetail ? `
                                <div style="margin-top: 10px; font-size: 0.9rem; color: #666; text-align: center;">
                                    ${product.campaignDetail}
                                </div>
                            ` : ''}
                        ` : ''}
                        
                        <h2 class="modal-title">${product.name}</h2>
                        
                        ${product.description ? `
                            <div class="modal-description">${product.description}</div>
                        ` : ''}
                        
                        <div class="modal-price">₺${product.price.toFixed(2)}</div>
                        
                        <div style="margin-top: 20px; text-align: center; color: #999; font-size: 0.85rem;">
                            <i class="fas fa-info-circle me-1"></i>
                            Detaylı bilgi için garsonunuza danışabilirsiniz
                        </div>
                    </div>
                </div>
            </div>
        `;

        document.body.insertAdjacentHTML('beforeend', modalHTML);
        document.getElementById('productModal').style.display = 'flex';
        document.body.style.overflow = 'hidden';
    }

    changeModalImage(targetIndex) {
        const modal = document.getElementById('productModal');
        const images = modal.querySelectorAll('.modal-image');
        const dots = modal.querySelectorAll('.modal-image-dot');

        images.forEach((img, index) => {
            img.classList.toggle('active', index === targetIndex);
        });

        dots.forEach((dot, index) => {
            dot.classList.toggle('active', index === targetIndex);
        });
    }

    closeProductModal() {
        const modal = document.getElementById('productModal');
        if (modal) {
            modal.style.display = 'none';
            modal.remove();
            document.body.style.overflow = '';
        }
    }

    // ===== UTILITY METHODS =====
    startClock() {
        const updateClock = () => {
            const now = new Date();
            const timeString = now.toLocaleTimeString('tr-TR', {
                hour: '2-digit',
                minute: '2-digit'
            });

            const timeElement = document.getElementById('currentTime');
            if (timeElement) {
                timeElement.textContent = timeString;
            }
        };

        updateClock();
        setInterval(updateClock, 1000);
    }

    updateWeatherDisplay() {
        const weatherElement = document.getElementById('weatherMini');
        if (!weatherElement || !this.weatherData) return;

        const temp = Math.round(this.weatherData.temperature || 20);
        const condition = this.weatherData.condition || 'clear';

        let icon = '🌤️';
        if (condition.includes('rain')) icon = '🌧️';
        else if (condition.includes('cloud')) icon = '☁️';
        else if (condition.includes('sun')) icon = '☀️';

        weatherElement.innerHTML = `${icon} ${temp}°`;
    }

    scheduleWeatherSuggestion() {
        if (this.suggestionShown || this.currentView !== 'categories') return;

        setTimeout(() => {
            if (this.currentView === 'categories') {
                this.showWeatherSuggestion();
            }
        }, 10000);
    }

    showWeatherSuggestion() {
        if (this.suggestionShown || this.currentView !== 'categories') return;

        const suggestion = this.generateWeatherSuggestion();
        if (!suggestion) return;

        const popupHTML = `
            <div class="weather-suggestion-popup" id="weatherSuggestion">
                <div class="suggestion-content">
                    <button class="suggestion-close" onclick="QRMenu.closeSuggestion()">×</button>
                    <div class="suggestion-body">
                        <div class="weather-icon">${suggestion.icon}</div>
                        <div class="suggestion-message">${suggestion.message}</div>
                        <div class="recommended-products">
                            ${suggestion.products.map(product =>
            `<span class="recommended-product">${product}</span>`
        ).join('')}
                        </div>
                    </div>
                </div>
            </div>
        `;

        document.body.insertAdjacentHTML('beforeend', popupHTML);
        document.getElementById('weatherSuggestion').style.display = 'flex';
        this.suggestionShown = true;

        setTimeout(() => {
            this.closeSuggestion();
        }, 15000);
    }

    generateWeatherSuggestion() {
        const now = new Date();
        const hour = now.getHours();
        const temp = this.weatherData?.temperature || 20;
        const condition = this.weatherData?.condition || '';

        if (temp < 10) {
            return {
                icon: '🥶',
                message: 'Soğuk bir gün! Sıcacık lezzetlerle ısınmaya ne dersin?',
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

        if (hour >= 6 && hour < 11) {
            return {
                icon: '🌅',
                message: 'Günaydın! Güne enerjik başlamak için neler var bakalım?',
                products: ['Americano', 'Croissant', 'Omlet', 'Fresh Orange']
            };
        }

        return null;
    }

    closeSuggestion() {
        const popup = document.getElementById('weatherSuggestion');
        if (popup) {
            popup.style.display = 'none';
            popup.remove();
        }
    }

    showError(message) {
        this.hideLoader();
        const container = document.getElementById('mainContent');
        container.innerHTML = `
            <div class="empty-state">
                <i class="fas fa-exclamation-triangle"></i>
                <h5 style="color: #dc3545;">Hata Oluştu</h5>
                <p>${message}</p>
                <button class="btn btn-primary mt-3" onclick="location.reload()">
                    <i class="fas fa-refresh me-2"></i>Sayfayı Yenile
                </button>
            </div>
        `;
    }

    static normalizeCategoryName(categoryName) {
        if (!categoryName) return "";

        return categoryName
            .toUpperCase()
            .replace(/Ç/g, "C")
            .replace(/Ğ/g, "G")
            .replace(/İ/g, "I")
            .replace(/Ö/g, "O")
            .replace(/Ş/g, "S")
            .replace(/Ü/g, "U")
            .replace(/\s+/g, "")
            .replace(/-/g, "")
            .replace(/_/g, "");
    }

    initializeFooter() {
        // Footer'a tıklanınca küçük bir easter egg
        const footer = document.querySelector('.developer-footer');
        if (footer) {
            let clickCount = 0;
            footer.addEventListener('click', () => {
                clickCount++;
                if (clickCount === 5) {
                    footer.style.background = 'linear-gradient(135deg, #ff6b6b, #4ecdc4)';
                    setTimeout(() => {
                        footer.style.background = 'linear-gradient(135deg, var(--primary-color) 0%, var(--secondary-color) 100%)';
                        clickCount = 0;
                    }, 2000);
                }
            });

            // Fareyle üzerine gelindiğinde küçük animasyon
            footer.addEventListener('mouseenter', () => {
                footer.style.transform = 'translateY(-3px)';
            });

            footer.addEventListener('mouseleave', () => {
                footer.style.transform = 'translateY(0)';
            });
        }
    }
}

// ===== GLOBAL INSTANCE =====
let QRMenu;

document.addEventListener('DOMContentLoaded', () => {
    QRMenu = new QRMenuEngine();
});

// Global fonksiyonlar
// Global fonksiyonlar
window.QRMenu = {
    selectCategory: (category) => QRMenu?.selectCategory(category),
    goBackToCategories: () => QRMenu?.goBackToCategories(),
    closeSuggestion: () => QRMenu?.closeSuggestion(),
    closeProductModal: () => QRMenu?.closeProductModal(),
    // ✅ Yeni carousel fonksiyonları
    changeProductImage: (productId, index) => QRMenu?.changeProductImage(productId, index),
    nextProductImage: (productId) => QRMenu?.nextProductImage(productId),
    previousProductImage: (productId) => QRMenu?.previousProductImage(productId),
    changeModalImage: (index) => QRMenu?.changeModalImage(index)
};