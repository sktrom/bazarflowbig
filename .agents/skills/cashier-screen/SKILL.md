# Skill: Cashier Screen Agent

## Overview
This skill embodies the knowledge required to comprehend, plan, and analyze a Cashier-first interface in the Angular point-of-sales context.

## Capabilities expected
- **Fast-Action UX Pattern Recognition:** Recognizing how layouts must facilitate extremely fast processing.
- **Visual implementation:** Adhering heavily to the `Emerald` approval color flow and large action button rules defined by the design system.
- **Barcode restrictions:** Enforcing logic that rejects multi-barcode binding mapping for a single product model (1 barcode = 1 product).

## Carton & Unit Handling Rules
- لا يوجد تبديل وحدة ظاهر داخل سطر الفاتورة. الوحدة ثابتة دائمًا.
- بيع الكرتونة يتم عبر:
  1. تعديل الكمية
  2. ثم تعديل إجمالي السطر (Line Total) فقط
- **لا يتم تغيير سعر الوحدة أبدًا** لتمثيل الكرتونة.

## Line Adjustment Reset
- إذا تغيّرت الكمية بعد تعديل إجمالي السطر يدويًا:
  1. يُلغى التعديل اليدوي على إجمالي السطر
  2. تُزال علامة التعديل (adjustment flag)
  3. يرجع الحساب الطبيعي (الكمية × سعر الوحدة)

## Customer Name Rule
- اسم الزبون اختياري بشكل افتراضي.
- يصبح **إلزاميًا** فقط إذا كانت الفاتورة المعلقة (Hold) بسبب مالي = دين (Debt).
