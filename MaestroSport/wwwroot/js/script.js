let currentProduct = null;
let appliedDiscount = 0;
let activeCouponCode = "";

// متغيرات السلة المصغرة
let selectedSizeId = null;
let selectedOrderItems = []; // مصفوفة لتخزين المقاسات والكميات المختارة

function toast(title, icon = 'success') {
    Swal.fire({
        title: title, icon: icon, timer: 3000, showConfirmButton: false, toast: true,
        position: 'top-end', background: '#151515', color: '#ffffff'
    });
}

function renderCategories() {
    const container = document.getElementById('categories-render');
    if (!container) return;
    container.innerHTML = "";
    categoriesData.forEach(cat => {
        container.innerHTML += `
            <div class="category-card" onclick="loadCategory(${cat.Id})">
                <div class="cat-icon ${cat.ColorClass}"><i class="fas ${cat.IconClass}"></i></div>
                <span style="color: #ffffff; font-weight: bold; font-size:1.2rem;">تفصيل ${cat.Name}</span>
            </div>`;
    });
}

function toggleView(screenId) {
    document.querySelectorAll('section').forEach(s => s.classList.add('hidden'));
    const targetScreen = document.getElementById(screenId + '-screen');
    if (targetScreen) targetScreen.classList.remove('hidden');
    if (screenId === 'categories') renderCategories();
    window.scrollTo({ top: 0, behavior: 'smooth' });
}

function loadCategory(catId) {
    const cat = categoriesData.find(c => c.Id === catId);
    if (!cat) return;

    document.getElementById('cat-display-name').innerText = "تفصيل " + cat.Name;
    const list = document.getElementById('products-list');
    list.innerHTML = "";

    if (!cat.Products || cat.Products.length === 0) {
        list.innerHTML = `<p style="text-align:center; width:100%; color:#D4AF37; font-size: 1.2rem;">لا توجد موديلات في هذا القسم حالياً.</p>`;
    } else {
        cat.Products.forEach(item => {
            list.innerHTML += `
                <div class="product-item">
                    <div class="product-

                    "><img src="/${item.ImageUrl}" onerror="this.src='https://placehold.co/400x500/111/D4AF37?text=No+Image'"></div>
                    <div class="product-info">
                        <h3 style="color: #ffffff;">${item.Name} ${item.IsCustomDesign ? '<br><small style="color:#D4AF37; font-size:0.8rem;">(تصميم خاص)</small>' : ''}</h3>
                        <p style="color:#D4AF37; font-weight:900; font-size:1.5rem; margin-bottom:15px;">${item.BasePrice} ريال</p>
                        <button class="btn-order" onclick="openOrderModal(${item.Id})"><i class="fas fa-magic"></i> اطلب تفصيل الآن</button>
                    </div>
                </div>`;
        });
    }
    toggleView('shop');
}

function openOrderModal(productId) {
    categoriesData.forEach(c => {
        const found = c.Products.find(p => p.Id === productId);
        if (found) currentProduct = found;
    });
    if (!currentProduct) return;

    document.getElementById('modal-item-name').innerText = "طلب تفصيل: " + currentProduct.Name;

    // إظهار/إخفاء المرفقات والتنبيه بناءً على نوع الموديل
    document.getElementById('custom-design-alert').classList.toggle('hidden', !currentProduct.IsCustomDesign);
    document.getElementById('custom-image-section').classList.toggle('hidden', !currentProduct.IsCustomDesign);

    // إعادة ضبط البيانات والسلة
    selectedOrderItems = [];
    selectedSizeId = null;
    document.getElementById('current-qty').value = 1;
    document.getElementById('custom-image-input').value = "";

    // رسم أزرار المقاسات
    const pillsContainer = document.getElementById('size-pills-container');
    pillsContainer.innerHTML = "";
    sizesData.forEach((s, index) => {
        let extra = parseFloat(s.AdditionalPrice);
        let extraText = extra > 0 ? `+${extra} ر.ع` : (extra < 0 ? `${extra} ر.ع` : "");
        let priceSpan = extraText ? `<span class="size-pill-price">${extraText}</span>` : "";

        // اختيار أول مقاس افتراضياً
        if (index === 0) selectedSizeId = s.Id;

        pillsContainer.innerHTML += `
            <div class="size-pill ${index === 0 ? 'selected' : ''}" onclick="selectSize(${s.Id}, this)">
                <span style="font-weight:bold; font-size:1.1rem;">${s.Name}</span>
                ${priceSpan}
            </div>
        `;
    });

    appliedDiscount = 0;
    activeCouponCode = "";
    document.getElementById('coupon-input').value = "";
    document.getElementById('order-notes').value = "";
    document.getElementById('fabric-select').value = "0";

    renderMiniCart();
    calculateLiveTotal();
    document.getElementById('order-modal').classList.remove('hidden');
}

// اختيار مقاس (UI)
function selectSize(id, element) {
    selectedSizeId = id;
    document.querySelectorAll('.size-pill').forEach(el => el.classList.remove('selected'));
    element.classList.add('selected');
}

// إضافة المقاس المختار للكميات
function addSelectedSize() {
    if (!selectedSizeId) return;
    const qty = parseInt(document.getElementById('current-qty').value);
    if (qty < 1) { toast("أدخل كمية صحيحة", "warning"); return; }

    const sizeObj = sizesData.find(s => s.Id === selectedSizeId);

    // التحقق إذا كان المقاس مضاف مسبقاً لزيادة الكمية بدلاً من التكرار
    const existingItem = selectedOrderItems.find(i => i.SizeId === selectedSizeId);
    if (existingItem) {
        existingItem.Quantity += qty;
    } else {
        selectedOrderItems.push({
            SizeId: sizeObj.Id,
            SizeName: sizeObj.Name,
            ExtraPrice: parseFloat(sizeObj.AdditionalPrice) || 0,
            Quantity: qty
        });
    }

    // إعادة الكمية لـ 1 بعد الإضافة
    document.getElementById('current-qty').value = 1;

    renderMiniCart();
    calculateLiveTotal();
    toast("تم الإضافة للسلة", "success");
}

function removeSize(index) {
    selectedOrderItems.splice(index, 1);
    renderMiniCart();
    calculateLiveTotal();
}

function renderMiniCart() {
    const container = document.getElementById('mini-cart-container');
    container.innerHTML = "";
    if (selectedOrderItems.length === 0) {
        container.innerHTML = `<p style="color:var(--text-dim); text-align:center; font-size:0.9rem;">لم تقم بإضافة أي مقاسات بعد.</p>`;
        return;
    }

    selectedOrderItems.forEach((item, index) => {
        container.innerHTML += `
            <div class="mini-cart-item">
                <div>
                    <span style="color:var(--gold); font-weight:bold;">${item.SizeName}</span>
                    <span style="margin:0 10px;">الكمية: <strong>${item.Quantity}</strong></span>
                </div>
                <button type="button" class="remove-item-btn" onclick="removeSize(${index})"><i class="fas fa-times-circle"></i></button>
            </div>
        `;
    });
}

function calculateLiveTotal(isCouponClick = false) {
    if (!currentProduct) return;

    let totalQty = 0;
    let totalPrice = 0;
    const fabricSelect = document.getElementById('fabric-select');
    const fabricExtra = fabricSelect ? parseFloat(fabricSelect.value) : 0;

    selectedOrderItems.forEach(item => {
        totalQty += item.Quantity;
        totalPrice += item.Quantity * (currentProduct.BasePrice + item.ExtraPrice + fabricExtra);
    });

    if (currentProduct.IsCustomDesign && totalQty > 0 && totalQty <= 10) {
        totalPrice += (2 * totalQty);
    }

    if (isCouponClick) {
        const codeInput = document.getElementById('coupon-input');
        const code = codeInput ? codeInput.value.trim().toUpperCase() : "";
        if (!code) return;

        fetch(`/Home/ValidateCoupon?code=${encodeURIComponent(code)}`)
            .then(res => res.json())
            .then(data => {
                if (data.valid) {
                    appliedDiscount = data.discount;
                    activeCouponCode = code;
                    Swal.fire({ title: 'تم تفعيل الخصم!', text: `حصلت على خصم ${data.discount} ريال`, icon: 'success', background: '#151515', color: '#fff' });
                } else {
                    appliedDiscount = 0; activeCouponCode = "";
                    Swal.fire({ title: 'عفواً!', text: 'كود غير صحيح', icon: 'error', background: '#151515', color: '#fff' });
                }
                updateDisplay();
            });
    } else {
        updateDisplay();
    }

    function updateDisplay() {
        let final = totalPrice - appliedDiscount;
        if (final < 0) final = 0;
        document.getElementById('total-price-display').innerText = final.toFixed(2);
    }

    return { totalQty, totalPrice: (totalPrice - appliedDiscount) };
}

function closeModal() { document.getElementById('order-modal').classList.add('hidden'); }

function submitOrderProcess() {
    const { totalQty, totalPrice } = calculateLiveTotal();
    if (totalQty === 0) {
        Swal.fire({ title: 'تنبيه', text: 'يرجى إضافة مقاس واحد على الأقل', icon: 'warning', background: '#151515', color: '#fff' });
        return;
    }

    Swal.fire({
        title: 'تأكيد الطلب الملكي',
        text: `أنت على وشك طلب ${totalQty} قطعة، هل تود المتابعة؟`,
        icon: 'question', showCancelButton: true, confirmButtonText: 'نعم، أرسل', cancelButtonText: 'تراجع',
        background: '#151515', color: '#ffffff', confirmButtonColor: '#25D366'
    }).then((result) => {
        if (result.isConfirmed) {
            const btn = document.getElementById('submit-btn');
            btn.disabled = true; btn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> جاري حفظ طلبك...';

            const fabricSelect = document.getElementById('fabric-select');
            const fabricName = fabricSelect.options[fabricSelect.selectedIndex].getAttribute('data-name');

            // استخدام FormData لدعم إرسال الملفات (الصور)
            const formData = new FormData();
            formData.append('ProductId', currentProduct.Id);
            formData.append('Notes', document.getElementById('order-notes').value || "");
            formData.append('FabricType', fabricName);
            formData.append('FabricExtraPrice', parseFloat(fabricSelect.value) || 0);
            formData.append('CouponCode', activeCouponCode);

            // إرسال السلة كـ JSON String
            formData.append('ItemsJson', JSON.stringify(selectedOrderItems.map(i => ({ SizeId: i.SizeId, Quantity: i.Quantity }))));

            // إضافة الصورة إذا وجدت
            const imageInput = document.getElementById('custom-image-input');
            if (imageInput && imageInput.files.length > 0) {
                formData.append('CustomImage', imageInput.files[0]);
            }

            fetch('/Home/SubmitOrder', {
                method: 'POST',
                body: formData // لا نضع Content-Type هنا لأن المتصفح سيضعه تلقائياً مع الـ Boundary
            })
                .then(res => res.json())
                .then(data => {
                    if (data.success) {
                        Swal.fire({ title: 'تم الحفظ بنجاح!', text: 'جاري تحويلك للواتساب...', icon: 'success', showConfirmButton: false, timer: 2000, background: '#151515', color: '#fff' })
                            .then(() => {
                                const msg = `*طلب تفصيل من المايسترو رقم ${data.orderId}*%0A*الموديل:* ${currentProduct.Name}%0A*القماش:* ${fabricName}%0A*القطع:* ${totalQty}%0A*الإجمالي النهائي:* ${data.totalPrice} ريال`;
                                window.location.href = `https://wa.me/96892111393?text=${msg}`;
                            });
                        closeModal();
                    } else {
                        Swal.fire('خطأ', data.message || 'حدثت مشكلة', 'error');
                    }
                })
                .catch(err => {
                    console.error(err);
                    Swal.fire('خطأ', 'فشل الاتصال بالخادم', 'error');
                })
                .finally(() => {
                    btn.disabled = false; btn.innerHTML = '<i class="fab fa-whatsapp"></i> اعتماد الطلب وإرسال للواتساب';
                });
        }
    });
}