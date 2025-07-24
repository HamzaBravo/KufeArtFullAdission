// ===== QR MENU ENGINE =====
class QRMenuEngine {
    constructor() {
        this.products = [];
        this.categories = [];
        this.currentView = 'categories';
        this.currentCategory = null;
        this.weatherData = null;
        this.suggestionShown = false;
        // 🎯 YENİ: Customer management
        this.currentCustomer = null;
        this.customerLoggedIn = false;

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
    // ✅ Çoklu resim destekli product card + PUAN BİLGİSİ
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
                                 onclick="QRMenu.changeProductImage('${product.id}', ${index})"></div>
                        `).join('')}
                    </div>
                    
                    <!-- Önceki/Sonraki butonları -->
                    <button class="carousel-btn carousel-prev" 
                            onclick="QRMenu.previousProductImage('${product.id}')">
                        <i class="fas fa-chevron-left"></i>
                    </button>
                    <button class="carousel-btn carousel-next" 
                            onclick="QRMenu.nextProductImage('${product.id}')">
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
                
                <!-- 🎯 YENİ: PUAN BADGE -->
                ${product.hasKufePoints && product.kufePoints > 0 ? `
                    <div class="points-badge">
                        <i class="fas fa-star"></i>
                        ${product.kufePoints}
                    </div>
                ` : ''}
            </div>
            
            <div class="product-info">
                <div class="product-name">${product.name}</div>
                ${product.description ? `
                    <div class="product-description">${product.description}</div>
                ` : ''}
                
                <!-- 🎯 YENİ: PUAN BİLGİSİ -->
                ${product.hasKufePoints && product.kufePoints > 0 ? `
                    <div class="product-points-info">
                        <i class="fas fa-gift me-1"></i>
                        Bu ürün <strong>${product.kufePoints} puan</strong> kazandırır
                    </div>
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
    // ===== MODAL METHODS =====
    // ✅ Modal'da da çoklu resim desteği + PUAN BİLGİSİ
    showProductModal(productId) {
        const product = this.products.find(p => p.id == productId);
        if (!product) return;

        const images = product.images && product.images.length > 0 ?
            product.images.filter(img => img && img.trim() !== '') : [''];

        const hasMultipleImages = images.length > 1 && images[0] !== '';

        const modalHTML = `
        <div class="product-modal" id="productModal">
            <div class="modal-content-custom">
                <div class="modal-header-custom">
                    ${hasMultipleImages ? `
                        <!-- Modal'da çoklu resim carousel -->
                        <div class="modal-image-carousel">
                            ${images.map((image, index) => `
                                <img src="${image}" alt="${product.name}" 
                                     class="modal-image ${index === 0 ? 'active' : ''}" 
                                     data-index="${index}">
                            `).join('')}
                        </div>
                        
                        <!-- Modal resim göstergeleri -->
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
                    
                    <button class="modal-close" onclick="QRMenu.closeProductModal()">×</button>
                </div>
                
                <div class="modal-body-custom">
                    ${product.hasCampaign ? `
                        <div class="modal-campaign">
                            🎯 ${product.campaignCaption || 'Özel Kampanya'}
                            ${product.campaignDetail ? `<br><small>${product.campaignDetail}</small>` : ''}
                        </div>
                    ` : ''}
                    
                    <h3 class="modal-title">${product.name}</h3>
                    
                    ${product.description ? `
                        <p class="modal-description">${product.description}</p>
                    ` : ''}
                    
                    <!-- 🎯 YENİ: MODAL'DA PUAN BİLGİSİ -->
                    ${product.hasKufePoints && product.kufePoints > 0 ? `
                        <div class="modal-points-info">
                            <div class="points-highlight">
                                <i class="fas fa-star"></i>
                                <span class="points-value">${product.kufePoints}</span>
                                <span class="points-text">puan kazanırsınız</span>
                            </div>
                            <div class="points-explanation">
                                ${this.customerLoggedIn ?
                    'Bu ürünü satın aldığınızda puanlarınız hesabınıza eklenecek!' :
                    'Puan kazanmak için üye girişi yapın!'
                }
                            </div>
                        </div>
                    ` : ''}
                    
                    <div class="modal-price">₺${product.price.toFixed(2)}</div>
                </div>
            </div>
        </div>
    `;

        document.body.insertAdjacentHTML('beforeend', modalHTML);
        document.getElementById('productModal').style.display = 'flex';
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

    // ===== CUSTOMER MANAGEMENT METHODS =====
    showLoginModal() {
        const modal = new bootstrap.Modal(document.getElementById('customerLoginModal'));
        this.showLoginForm(); // Varsayılan olarak login formunu göster
        modal.show();
    }

    showCustomerModal() {
        if (!this.currentCustomer) return;

        this.loadCustomerProfile();
        const modal = new bootstrap.Modal(document.getElementById('customerProfileModal'));
        modal.show();
    }

    showLoginForm() {
        document.getElementById('loginForm').style.display = 'block';
        document.getElementById('registerForm').style.display = 'none';

        // Input'ları temizle
        document.getElementById('loginPhone').value = '';
    }

    showRegisterForm() {
        document.getElementById('loginForm').style.display = 'none';
        document.getElementById('registerForm').style.display = 'block';

        // Input'ları temizle
        document.getElementById('registerName').value = '';
        document.getElementById('registerPhone').value = '';
    }

    async loginCustomer() {
        const phoneInput = document.getElementById('loginPhone');
        const phoneNumber = phoneInput.value.trim();

        if (!this.validatePhoneNumber(phoneNumber)) {
            this.showToast('Geçerli bir telefon numarası girin! (05XX XXX XX XX)', 'error');
            return;
        }

        try {
            this.showLoadingInModal('Giriş yapılıyor...');

            const response = await fetch('/api/customer/login', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ phoneNumber: phoneNumber })
            });

            const data = await response.json();

            if (data.success) {
                this.currentCustomer = data.customer;
                this.customerLoggedIn = true;
                this.updateCustomerUI();
                this.hideModal('customerLoginModal');
                this.showToast(`Hoşgeldiniz ${data.customer.fullname}!`, 'success');
            } else {
                if (data.shouldRegister) {
                    // Telefonu register formuna aktar
                    document.getElementById('registerPhone').value = phoneNumber;
                    this.showRegisterForm();
                    this.showToast('Bu numaraya kayıtlı hesap bulunamadı. Lütfen kayıt olun.', 'info');
                } else {
                    this.showToast(data.message, 'error');
                }
            }
        } catch (error) {
            console.error('Login error:', error);
            this.showToast('Giriş sırasında hata oluştu!', 'error');
        } finally {
            this.hideLoadingInModal();
        }
    }

    async registerCustomer() {
        const nameInput = document.getElementById('registerName');
        const phoneInput = document.getElementById('registerPhone');

        const fullname = nameInput.value.trim();
        const phoneNumber = phoneInput.value.trim();

        if (!fullname) {
            this.showToast('Lütfen adınızı ve soyadınızı girin!', 'error');
            return;
        }

        if (!this.validatePhoneNumber(phoneNumber)) {
            this.showToast('Geçerli bir telefon numarası girin! (05XX XXX XX XX)', 'error');
            return;
        }

        try {
            this.showLoadingInModal('Kayıt yapılıyor...');

            const response = await fetch('/api/customer/register', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    fullname: fullname,
                    phoneNumber: phoneNumber
                })
            });

            const data = await response.json();

            if (data.success) {
                this.currentCustomer = data.customer;
                this.customerLoggedIn = true;
                this.updateCustomerUI();
                this.hideModal('customerLoginModal');
                this.showToast(`Kayıt başarılı! Hoşgeldiniz ${data.customer.fullname}!`, 'success');
            } else {
                this.showToast(data.message, 'error');
            }
        } catch (error) {
            console.error('Register error:', error);
            this.showToast('Kayıt sırasında hata oluştu!', 'error');
        } finally {
            this.hideLoadingInModal();
        }
    }

    async loadCustomerProfile() {
        if (!this.currentCustomer) return;

        try {
            // Güncel puan bilgisini al
            const pointsResponse = await fetch(`/api/customer/${this.currentCustomer.id}/points`);
            const pointsData = await pointsResponse.json();

            if (pointsData.success) {
                this.currentCustomer = pointsData.customer;
                this.updateProfileModal();
                this.loadCustomerHistory();
            }
        } catch (error) {
            console.error('Profile load error:', error);
        }
    }

    async loadCustomerHistory() {
        if (!this.currentCustomer) return;

        try {
            const response = await fetch(`/api/customer/${this.currentCustomer.id}/points-history?limit=10`);
            const data = await response.json();

            if (data.success) {
                this.updateTransactionHistory(data.history);
            }
        } catch (error) {
            console.error('History load error:', error);
            document.getElementById('recentTransactions').innerHTML = `
                <div class="text-muted text-center py-3">
                    <i class="fas fa-exclamation-circle"></i>
                    <br>
                    Geçmiş yüklenemedi
                </div>
            `;
        }
    }

    updateCustomerUI() {
        const loginPrompt = document.getElementById('loginPrompt');
        const customerInfo = document.getElementById('customerInfo');

        if (this.customerLoggedIn && this.currentCustomer) {
            loginPrompt.style.display = 'none';
            customerInfo.style.display = 'flex';

            document.getElementById('customerName').textContent = this.currentCustomer.fullname;
            document.getElementById('customerPoints').innerHTML = `
                <i class="fas fa-star me-1"></i>
                <span class="points-value">${this.currentCustomer.totalPoints}</span> puan
            `;
        } else {
            loginPrompt.style.display = 'block';
            customerInfo.style.display = 'none';
        }
    }

    updateProfileModal() {
        if (!this.currentCustomer) return;

        document.getElementById('profileName').textContent = this.currentCustomer.fullname;
        document.getElementById('profilePhone').textContent = this.currentCustomer.phoneNumber;
        document.getElementById('profilePoints').textContent = this.currentCustomer.totalPoints;

        const pointsInfo = document.getElementById('pointsUsableInfo');
        if (this.currentCustomer.totalPoints >= 5000) {
            pointsInfo.textContent = `${Math.floor(this.currentCustomer.totalPoints / 100)}TL indirim hakkınız var!`;
            pointsInfo.className = 'text-success';
        } else {
            const needed = 5000 - this.currentCustomer.totalPoints;
            pointsInfo.textContent = `${needed} puan daha toplayın, 50TL indirim kazanın!`;
            pointsInfo.className = 'text-warning';
        }
    }

    updateTransactionHistory(transactions) {
        const container = document.getElementById('recentTransactions');

        if (!transactions || transactions.length === 0) {
            container.innerHTML = `
                <div class="text-muted text-center py-3">
                    <i class="fas fa-inbox"></i>
                    <br>
                    Henüz işlem geçmişi yok
                </div>
            `;
            return;
        }

        const transactionsHTML = transactions.map(transaction => `
            <div class="transaction-item">
                <div>
                    <div class="transaction-description">${transaction.description}</div>
                    <div class="transaction-date">${transaction.date}</div>
                </div>
                <div class="transaction-type ${transaction.type.toLowerCase()}">
                    ${transaction.type === 'Earned' ? '+' : '-'}${transaction.points} puan
                </div>
            </div>
        `).join('');

        container.innerHTML = transactionsHTML;
    }

    logoutCustomer() {
        this.currentCustomer = null;
        this.customerLoggedIn = false;
        this.updateCustomerUI();
        this.hideModal('customerProfileModal');
        this.showToast('Çıkış yapıldı!', 'info');
    }

    // ===== UTILITY METHODS =====
    validatePhoneNumber(phone) {
        return phone && phone.length === 11 && phone.startsWith('0') && /^\d+$/.test(phone);
    }

    showLoadingInModal(message = 'Yükleniyor...') {
        const modalBodies = document.querySelectorAll('.modal-body');
        modalBodies.forEach(body => {
            const loading = body.querySelector('.modal-loading');
            if (!loading) {
                body.insertAdjacentHTML('beforeend', `
                    <div class="modal-loading position-absolute top-0 start-0 w-100 h-100 
                         d-flex align-items-center justify-content-center bg-white bg-opacity-75">
                        <div class="text-center">
                            <div class="spinner-border text-primary mb-2" role="status"></div>
                            <div>${message}</div>
                        </div>
                    </div>
                `);
            }
        });
    }

    hideLoadingInModal() {
        const loadings = document.querySelectorAll('.modal-loading');
        loadings.forEach(loading => loading.remove());
    }

    hideModal(modalId) {
        const modal = bootstrap.Modal.getInstance(document.getElementById(modalId));
        if (modal) modal.hide();
    }

    showToast(message, type = 'info') {
        // Toast sistemi için basit bir implementasyon
        const toast = document.createElement('div');
        toast.className = `toast-notification toast-${type}`;
        toast.innerHTML = `
            <div class="toast-content">
                <i class="fas fa-${this.getToastIcon(type)} me-2"></i>
                ${message}
            </div>
        `;

        toast.style.cssText = `
            position: fixed;
            top: 20px;
            right: 20px;
            z-index: 9999;
            padding: 12px 20px;
            border-radius: 8px;
            color: white;
            font-weight: 500;
            transform: translateX(100%);
            transition: transform 0.3s ease;
            background: ${this.getToastColor(type)};
            box-shadow: 0 4px 12px rgba(0,0,0,0.15);
        `;

        document.body.appendChild(toast);

        setTimeout(() => {
            toast.style.transform = 'translateX(0)';
        }, 100);

        setTimeout(() => {
            toast.style.transform = 'translateX(100%)';
            setTimeout(() => toast.remove(), 300);
        }, 3000);
    }

    getToastIcon(type) {
        const icons = {
            success: 'check-circle',
            error: 'exclamation-circle',
            warning: 'exclamation-triangle',
            info: 'info-circle'
        };
        return icons[type] || 'info-circle';
    }

    getToastColor(type) {
        const colors = {
            success: '#28a745',
            error: '#dc3545',
            warning: '#ffc107',
            info: '#17a2b8'
        };
        return colors[type] || '#17a2b8';
    }
}

// ===== GLOBAL INSTANCE =====
let QRMenu;

document.addEventListener('DOMContentLoaded', () => {
    QRMenu = new QRMenuEngine();
});

// Global fonksiyonlar
window.QRMenu = {
    selectCategory: (category) => QRMenu?.selectCategory(category),
    goBackToCategories: () => QRMenu?.goBackToCategories(),
    closeSuggestion: () => QRMenu?.closeSuggestion(),
    closeProductModal: () => QRMenu?.closeProductModal(),
    // ✅ Carousel fonksiyonları
    changeProductImage: (productId, index) => QRMenu?.changeProductImage(productId, index),
    nextProductImage: (productId) => QRMenu?.nextProductImage(productId),
    previousProductImage: (productId) => QRMenu?.previousProductImage(productId),
    changeModalImage: (index) => QRMenu?.changeModalImage(index),
    // 🎯 YENİ: Customer management fonksiyonları
    showLoginModal: () => QRMenu?.showLoginModal(),
    showCustomerModal: () => QRMenu?.showCustomerModal(),
    showLoginForm: () => QRMenu?.showLoginForm(),
    showRegisterForm: () => QRMenu?.showRegisterForm(),
    loginCustomer: () => QRMenu?.loginCustomer(),
    registerCustomer: () => QRMenu?.registerCustomer(),
    logoutCustomer: () => QRMenu?.logoutCustomer()
};