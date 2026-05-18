# Known Limitations

* **Reports Export:** تصدير التقارير غير منفذ حاليًا إذا لم يكن موجودًا في الواجهة.
* **Print / PDF:** طباعة الفواتير أو تصديرها كـ PDF غير منفذ حاليًا.
* **Offers Product Selection:** في الإصدار V1، يعتمد اختيار المنتج في العروض على إدخال الـ Product ID يدويًا، مع عرض رقم المنتج في شاشتي المنتجات (Products) والمخزون (Inventory) لتسهيل الوصول إليه.
* **Pagination:** لا توجد pagination متقدمة في بعض الجداول.
* **Device Code:** المتغير `deviceCode` في V1 ثابت كـ `DEFAULT_DEVICE`.
* **Store Settings:** إعدادات المتجر مثل `StoreName` و `ExchangeRate` في شاشة Settings معروضة للقراءة فقط (read-only) حاليًا من الواجهة.
* **Production Readiness:** هذه النسخة مناسبة للتشغيل المحلي/التجريبي وليست production نهائية، ويُنصح بعدم إطلاقها للجمهور بدون مراجعة أمنية شاملة لبيئة الإنتاج.
