import { ComponentFixture, TestBed } from '@angular/core/testing';
import { SettingsComponent } from './settings.component';
import { SettingsApiService } from '../services/settings-api.service';
import { of, throwError } from 'rxjs';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';

describe('SettingsComponent', () => {
  let component: SettingsComponent;
  let fixture: ComponentFixture<SettingsComponent>;
  let apiSpy: jasmine.SpyObj<SettingsApiService>;

  const mockEmployees = [
    { id: 1, fullName: 'Ali', username: 'ali', isActive: true, createdAt: '2024-01-01', phone: null }
  ];
  const mockDetail = {
    id: 1, fullName: 'Ali', username: 'ali', isActive: true, createdAt: '', updatedAt: '', phone: null,
    permissions: [
      { screenId: 1, screenKey: 'Sales', screenName: 'Sales', canAccess: true },
      { screenId: 2, screenKey: 'Products', screenName: 'Products', canAccess: false }
    ]
  };

  beforeEach(async () => {
    const spy = jasmine.createSpyObj('SettingsApiService', [
      'getEmployees','getEmployee','createEmployee','updateEmployee','deleteEmployee','resetPassword',
      'getCategories','createCategory','updateCategory','deleteCategory','getPublicSettings','createBackup'
    ]);
    spy.getEmployees.and.returnValue(of({ items: mockEmployees }));
    spy.getCategories.and.returnValue(of({ items: [] }));
    spy.getPublicSettings.and.returnValue(of({ storeName: 'Test Store', exchangeRate: 15000 }));

    await TestBed.configureTestingModule({
      imports: [SettingsComponent, FormsModule],
      providers: [{ provide: SettingsApiService, useValue: spy }]
    }).compileComponents();

    apiSpy = TestBed.inject(SettingsApiService) as jasmine.SpyObj<SettingsApiService>;
    fixture = TestBed.createComponent(SettingsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should load employees on init (default tab)', () => {
    expect(apiSpy.getEmployees).toHaveBeenCalled();
    expect(component.employees.length).toBe(1);
    expect(component.activeTab).toBe('employees');
  });

  it('should show empty state for categories', () => {
    component.switchTab('categories');
    expect(apiSpy.getCategories).toHaveBeenCalled();
    expect(component.categories.length).toBe(0);
  });

  it('should show store settings on store tab', () => {
    component.switchTab('store');
    expect(apiSpy.getPublicSettings).toHaveBeenCalled();
    expect(component.storeSettings?.storeName).toBe('Test Store');
    expect(component.storeSettings?.exchangeRate).toBe(15000);
  });

  it('should render backup tab', () => {
    component.switchTab('backup');
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('النسخ الاحتياطي');
    expect(text).toContain('إنشاء نسخة احتياطية');
  });

  it('should call create backup and render success metadata', () => {
    apiSpy.createBackup.and.returnValue(of({
      success: true,
      fileName: 'BazarFlow_Backup_20260520_143012.bak',
      createdAt: '2026-05-20T14:30:12',
      sizeBytes: 1048576,
      message: 'Backup created successfully.',
      backupDirectory: 'C:\\BazarFlowBackups'
    }));

    component.switchTab('backup');
    component.createBackup();
    fixture.detectChanges();

    expect(apiSpy.createBackup).toHaveBeenCalled();
    const text = fixture.nativeElement.textContent;
    expect(text).toContain('BazarFlow_Backup_20260520_143012.bak');
    expect(text).toContain('C:\\BazarFlowBackups');
    expect(text).toContain('1.00 MB');
  });

  it('should render backup error message', () => {
    const err = new HttpErrorResponse({ error: { error: 'BACKUP_SQL_FAILED' }, status: 500 });
    apiSpy.createBackup.and.returnValue(throwError(() => err));

    component.switchTab('backup');
    component.createBackup();
    fixture.detectChanges();

    expect(component.backupError).toBe('فشل تنفيذ النسخ الاحتياطي في SQL Server');
    expect(fixture.nativeElement.textContent).toContain('فشل تنفيذ النسخ الاحتياطي في SQL Server');
  });

  it('should open create employee modal', () => {
    component.openEmpCreate();
    expect(component.activeModal).toBe('empCreate');
    expect(component.editEmpId).toBeNull();
  });

  it('should load permissions when opening edit modal', () => {
    apiSpy.getEmployee.and.returnValue(of(mockDetail as any));
    component.openEmpEdit(mockEmployees[0] as any);
    expect(apiSpy.getEmployee).toHaveBeenCalledWith(1);
    expect(component.empPermissions['Sales']).toBeTrue();
    expect(component.empPermissions['Products']).toBeFalse();
    expect(component.activeModal).toBe('empEdit');
  });

  it('should toggle permission checkbox', () => {
    component.empPermissions = { Sales: true };
    component.togglePermission('Sales');
    expect(component.empPermissions['Sales']).toBeFalse();
  });

  it('should map USERNAME_ALREADY_EXISTS error in create', () => {
    const err = new HttpErrorResponse({ error: { error: 'USERNAME_ALREADY_EXISTS' }, status: 409 });
    apiSpy.createEmployee.and.returnValue(throwError(() => err));
    component.empForm = { fullName: 'Ali', username: 'ali', password: '123' };
    component.activeModal = 'empCreate';
    component.saveEmployee();
    expect(component.formErr).toBe('اسم المستخدم موجود مسبقًا');
  });

  it('should show CANNOT_DELETE_SELF as toast on delete', () => {
    const err = new HttpErrorResponse({ error: { error: 'CANNOT_DELETE_SELF' }, status: 409 });
    apiSpy.deleteEmployee.and.returnValue(throwError(() => err));
    component.actionEmp = mockEmployees[0] as any;
    component.activeModal = 'empDelete';
    component.confirmDeleteEmployee();
    expect(component.toast).toBe('لا يمكنك حذف حسابك الحالي');
  });

  it('should open category create modal', () => {
    component.openCatCreate();
    expect(component.activeModal).toBe('catCreate');
    expect(component.catForm.name).toBe('');
  });

  it('should map CATEGORY_NAME_ALREADY_EXISTS error', () => {
    const err = new HttpErrorResponse({ error: { error: 'CATEGORY_NAME_ALREADY_EXISTS' }, status: 409 });
    apiSpy.createCategory.and.returnValue(throwError(() => err));
    component.catForm = { name: 'Electronics' };
    component.activeModal = 'catCreate';
    component.saveCategory();
    expect(component.formErr).toBe('اسم التصنيف موجود مسبقًا');
  });
});
