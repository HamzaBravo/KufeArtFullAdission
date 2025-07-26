// Image Processing Progress Helper
window.ImageProgressHelper = {

    // Progress container'ı oluştur
    createProgressContainer: function (containerId) {
        const container = document.getElementById(containerId);
        if (!container) return null;

        // Eğer zaten varsa tekrar oluşturma
        if (document.getElementById('imageProgress')) {
            return document.getElementById('imageProgress');
        }

        const progressHtml = `
            <div class="image-upload-progress" id="imageProgress">
                <div class="progress-header">
                    <h6 class="progress-title">
                        <i class="fas fa-image me-2"></i>Resimler İşleniyor
                    </h6>
                    <span class="progress-percentage">0%</span>
                </div>
                <div class="progress-bar-container">
                    <div class="progress-bar-fill" id="progressBarFill"></div>
                </div>
                <p class="processing-files">
                    <span id="currentFile">0</span> / <span id="totalFiles">0</span> dosya işlendi
                </p>
                <div class="file-status" id="fileStatus"></div>
            </div>
        `;

        container.insertAdjacentHTML('afterend', progressHtml);
        return document.getElementById('imageProgress');
    },

    // Progress'i göster
    show: function () {
        const progress = document.getElementById('imageProgress');
        if (progress) {
            progress.style.display = 'block';
        }
    },

    // Progress'i gizle
    hide: function () {
        const progress = document.getElementById('imageProgress');
        if (progress) {
            progress.style.display = 'none';
        }
    },

    // Progress'i güncelle
    updateProgress: function (current, total, fileName = '') {
        const percentage = Math.round((current / total) * 100);

        // Progress bar'ı güncelle
        const progressBar = document.getElementById('progressBarFill');
        if (progressBar) {
            progressBar.style.width = percentage + '%';
        }

        // Yüzdeyi güncelle
        const percentageElement = document.querySelector('.progress-percentage');
        if (percentageElement) {
            percentageElement.textContent = percentage + '%';
        }

        // Dosya sayısını güncelle
        const currentFileElement = document.getElementById('currentFile');
        const totalFileElement = document.getElementById('totalFiles');
        if (currentFileElement) currentFileElement.textContent = current;
        if (totalFileElement) totalFileElement.textContent = total;
    },

    // Dosya durumu ekle
    addFileStatus: function (fileName, status) {
        const fileStatus = document.getElementById('fileStatus');
        if (!fileStatus) return;

        const icons = {
            'processing': 'fas fa-spinner fa-spin text-warning',
            'completed': 'fas fa-check-circle text-success',
            'error': 'fas fa-times-circle text-danger'
        };

        const statusTexts = {
            'processing': 'İşleniyor...',
            'completed': 'Tamamlandı',
            'error': 'Hata oluştu'
        };

        const existingItem = fileStatus.querySelector(`[data-file="${fileName}"]`);

        if (existingItem) {
            // Mevcut öğeyi güncelle
            existingItem.innerHTML = `
                <i class="${icons[status]}"></i>
                <span>${fileName} - ${statusTexts[status]}</span>
            `;
            existingItem.className = `file-item file-${status}`;
        } else {
            // Yeni öğe ekle
            const fileItem = document.createElement('div');
            fileItem.className = `file-item file-${status}`;
            fileItem.setAttribute('data-file', fileName);
            fileItem.innerHTML = `
                <i class="${icons[status]}"></i>
                <span>${fileName} - ${statusTexts[status]}</span>
            `;
            fileStatus.appendChild(fileItem);
        }
    },

    // Progress'i sıfırla
    reset: function () {
        const progressBar = document.getElementById('progressBarFill');
        const percentageElement = document.querySelector('.progress-percentage');
        const currentFileElement = document.getElementById('currentFile');
        const fileStatus = document.getElementById('fileStatus');

        if (progressBar) progressBar.style.width = '0%';
        if (percentageElement) percentageElement.textContent = '0%';
        if (currentFileElement) currentFileElement.textContent = '0';
        if (fileStatus) fileStatus.innerHTML = '';
    },

    // ✅ ERR_UPLOAD_FILE_CHANGED HATASINI ÖNLEYEN GÜVENLI FILE READER
    safeFileReader: function (file) {
        return new Promise((resolve, reject) => {
            if (!file || !file.type.startsWith('image/')) {
                reject(new Error('Geçersiz dosya türü'));
                return;
            }

            const reader = new FileReader();

            // Timeout ekle (büyük dosyalar için)
            const timeout = setTimeout(() => {
                reader.abort();
                reject(new Error('Dosya okuma zaman aşımı'));
            }, 10000); // 10 saniye timeout

            reader.onload = function (e) {
                clearTimeout(timeout);
                resolve(e.target.result);
            };

            reader.onerror = function () {
                clearTimeout(timeout);
                reject(new Error('Dosya okunamadı'));
            };

            reader.onabort = function () {
                clearTimeout(timeout);
                reject(new Error('Dosya okuma iptal edildi'));
            };

            // Güvenli okuma
            try {
                reader.readAsDataURL(file);
            } catch (error) {
                clearTimeout(timeout);
                reject(error);
            }
        });
    },

    // Dosyaları işle (ERR_UPLOAD_FILE_CHANGED'i önleyerek)
    processFiles: async function (files, previewContainer) {
        if (!files || files.length === 0) return;

        this.show();
        this.reset();

        const totalFiles = files.length;
        let processedFiles = 0;

        // Preview container'ı temizle
        if (previewContainer) {
            previewContainer.innerHTML = '';
        }

        for (let i = 0; i < files.length; i++) {
            const file = files[i];

            try {
                // Dosya işleme başla
                this.addFileStatus(file.name, 'processing');

                // ✅ GÜVENLI DOSYA OKUMA
                const dataUrl = await this.safeFileReader(file);

                // Preview oluştur
                if (previewContainer) {
                    const preview = document.createElement('div');
                    preview.className = 'image-preview-item d-inline-block me-2 mb-2';
                    preview.innerHTML = `
                        <div class="position-relative">
                            <img src="${dataUrl}" alt="Preview"
                                 style="width: 100px; height: 100px; object-fit: cover; border-radius: 8px; border: 2px solid #28a745;">
                            <small class="d-block text-center mt-1 text-muted">${file.name}</small>
                        </div>
                    `;
                    previewContainer.appendChild(preview);
                }

                // Yapay gecikme (processing simulation)
                await this.delay(300 + Math.random() * 500);

                // Dosya tamamlandı
                processedFiles++;
                this.updateProgress(processedFiles, totalFiles);
                this.addFileStatus(file.name, 'completed');

            } catch (error) {
                console.error('Dosya işleme hatası:', error);
                this.addFileStatus(file.name, 'error');
                processedFiles++;
                this.updateProgress(processedFiles, totalFiles);
            }
        }

        // Tümü tamamlandı, 2 saniye sonra gizle
        setTimeout(() => {
            this.hide();
        }, 2000);
    },

    // Gecikme fonksiyonu
    delay: function (ms) {
        return new Promise(resolve => setTimeout(resolve, ms));
    }
};