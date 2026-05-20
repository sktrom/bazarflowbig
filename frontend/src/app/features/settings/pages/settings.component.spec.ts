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
      'getCategories','createCategory','updateCategory','deleteCategory','getPublicSettings','createBackup',
      'getAuditLogs','getAuditLog',
      'getDevices','getDevice','createDevice','updateDevice','enableDevice','disableDevice','deleteDevice'
    ]);
    spy.getEmployees.and.returnValue(of({ items: mockEmployees }));
    spy.getCategories.and.returnValue(of({ items: [] }));
    spy.getPublicSettings.and.returnValue(of({ storeName: 'Test Store', exchangeRate: 15000 }));
    spy.getAuditLogs.and.returnValue(of({ items: [], totalCount: 0, page: 1, pageSize: 50 }));
    spy.getAuditLog.and.returnValue(of({ id: 1, action: 'CREATE', entityType: 'Product', createdAt: '2026-05-20T14:30:12', hasBefore: false, hasAfter: false, hasMetadata: false }));
    spy.getDevices.and.returnValue(of([]));
    spy.getDevice.and.returnValue(of({ id: 1, deviceCode: 'POS-01', deviceName: 'Register 1', isActive: true, notes: '', createdAt: '', updatedAt: '' }));
    spy.createDevice.and.returnValue(of({ id: 1, deviceCode: 'POS-01', deviceName: 'Register 1', isActive: true, notes: '', createdAt: '', updatedAt: '' }));
    spy.updateDevice.and.returnValue(of({ id: 1, deviceCode: 'POS-01', deviceName: 'Register 1', isActive: true, notes: '', createdAt: '', updatedAt: '' }));
    spy.enableDevice.and.returnValue(of({ success: true, message: 'Device enabled' }));
    spy.disableDevice.and.returnValue(of({ success: true, message: 'Device disabled' }));
    spy.deleteDevice.and.returnValue(of({ success: true, message: 'DEVICE_DELETED' }));

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

  it('should render audit logs tab and switch successfully', () => {
    component.switchTab('audit');
    expect(component.activeTab).toBe('audit');
    expect(apiSpy.getAuditLogs).toHaveBeenCalled();
  });

  it('should load audit logs with filter params when filtered', () => {
    component.switchTab('audit');
    component.filterAction = 'CREATE';
    component.filterEntityType = 'Product';
    component.filterEmployeeId = 5;
    component.filterDateFrom = '2026-05-01';
    component.filterDateTo = '2026-05-31';

    component.loadAuditLogs();
    
    expect(apiSpy.getAuditLogs).toHaveBeenCalledWith({
      page: 1,
      pageSize: 50,
      action: 'CREATE',
      entityType: 'Product',
      employeeId: 5,
      dateFrom: new Date('2026-05-01').toISOString(),
      dateTo: new Date('2026-05-31').toISOString()
    });
  });

  it('should paginate to next page', () => {
    component.auditPage = 1;
    component.auditPageSize = 50;
    component.auditTotalCount = 120;
    
    component.changeAuditPage(1);
    expect(component.auditPage).toBe(2);
    expect(apiSpy.getAuditLogs).toHaveBeenCalled();
  });

  it('should open audit detail modal and show detail response', () => {
    const mockDetailResponse = {
      id: 77,
      action: 'CREATE_BACKUP',
      entityType: 'Backup',
      createdAt: '2026-05-20T14:30:12',
      hasBefore: false,
      hasAfter: false,
      hasMetadata: true,
      metadataJson: '{"file":"test.bak"}'
    };
    apiSpy.getAuditLog.and.returnValue(of(mockDetailResponse));

    component.openAuditDetail(77);
    expect(apiSpy.getAuditLog).toHaveBeenCalledWith(77);
    expect(component.selectedAuditLog?.action).toBe('CREATE_BACKUP');
    expect(component.activeModal).toBe('auditDetail');
  });

  it('should display empty state when audit logs count is zero', () => {
    component.switchTab('audit');
    component.auditLogs = [];
    fixture.detectChanges();
    const text = fixture.nativeElement.textContent;
    expect(text).toContain('لا توجد سجلات مطابقة للفلاتر المحددة.');
  });

  it('should translate actions to Arabic', () => {
    expect(component.translateAction('CREATE_BACKUP')).toBe('إنشاء نسخة احتياطية');
    expect(component.translateAction('RESET_PASSWORD')).toBe('إعادة تعيين كلمة المرور');
    expect(component.translateAction('UNKNOWN_ACTION')).toBe('UNKNOWN_ACTION');
  });

  it('should switch to devices tab and load devices list', () => {
    const mockDevices = [
      { id: 10, deviceCode: 'POS-10', deviceName: 'Counter 10', isActive: true, createdAt: '2026-05-19T23:00:00' }
    ];
    apiSpy.getDevices.and.returnValue(of(mockDevices));

    component.switchTab('devices');
    expect(component.activeTab).toBe('devices');
    expect(apiSpy.getDevices).toHaveBeenCalled();
    expect(component.devices.length).toBe(1);
    expect(component.devices[0].deviceName).toBe('Counter 10');
  });

  it('should open create device modal', () => {
    component.openDeviceCreate();
    expect(component.activeModal).toBe('deviceCreate');
    expect(component.editDeviceId).toBeNull();
    expect(component.deviceForm.deviceCode).toBe('');
  });

  it('should save new device successfully', () => {
    component.activeModal = 'deviceCreate';
    component.deviceForm = { deviceCode: 'POS-NEW', deviceName: 'New POS Register', notes: 'Main area' };
    
    component.saveDevice();
    expect(apiSpy.createDevice).toHaveBeenCalledWith({
      deviceCode: 'POS-NEW',
      deviceName: 'New POS Register',
      notes: 'Main area'
    });
  });

  it('should toggle device activation state', () => {
    const d = { id: 10, deviceCode: 'POS-10', deviceName: 'Counter 10', isActive: true, createdAt: '2026-05-19T23:00:00' };
    component.toggleDeviceActive(d);
    expect(apiSpy.disableDevice).toHaveBeenCalledWith(10);
  });

  it('should delete device or show deactivated toast if used', () => {
    component.actionDevice = { id: 12, deviceCode: 'POS-12', deviceName: 'Counter 12', isActive: true, createdAt: '2026-05-19' };
    component.activeModal = 'deviceDelete';
    
    // Test physical delete success
    apiSpy.deleteDevice.and.returnValue(of({ success: true, message: 'DEVICE_DELETED' }));
    component.confirmDeleteDevice();
    expect(apiSpy.deleteDevice).toHaveBeenCalledWith(12);
    expect(component.toast).toBe('تم حذف الجهاز بنجاح');
  });

  it('should show default device badge in HTML template when deviceCode is DEFAULT_DEVICE', () => {
    component.switchTab('devices');
    component.devices = [
      { id: 1, deviceCode: 'DEFAULT_DEVICE', deviceName: 'Default Register', isActive: true, createdAt: '2026-05-19' }
    ];
    fixture.detectChanges();
    const compiled = fixture.nativeElement;
    expect(compiled.textContent).toContain('جهاز النظام الافتراضي');
  });

  it('should show toast error message when deleting DEFAULT_DEVICE', () => {
    const d = { id: 1, deviceCode: 'DEFAULT_DEVICE', deviceName: 'Default Register', isActive: true, createdAt: '2026-05-19' };
    component.openDeviceDelete(d);
    expect(component.toast).toBe('لا يمكن حذف جهاز النظام الافتراضي');
  });

  it('should map CANNOT_DELETE_DEFAULT_DEVICE error and display in toast', () => {
    const err = new HttpErrorResponse({ error: { error: 'CANNOT_DELETE_DEFAULT_DEVICE' }, status: 400 });
    apiSpy.deleteDevice.and.returnValue(throwError(() => err));
    component.actionDevice = { id: 1, deviceCode: 'DEFAULT_DEVICE', deviceName: 'Default Register', isActive: true, createdAt: '2026-05-19' };
    component.activeModal = 'deviceDelete';
    component.confirmDeleteDevice();
    expect(component.toast).toBe('لا يمكن حذف جهاز النظام الافتراضي');
  });

  it('should map CANNOT_DISABLE_LAST_ACTIVE_DEVICE error and display in toast', () => {
    const err = new HttpErrorResponse({ error: { error: 'CANNOT_DISABLE_LAST_ACTIVE_DEVICE' }, status: 400 });
    apiSpy.disableDevice.and.returnValue(throwError(() => err));
    const d = { id: 10, deviceCode: 'POS-10', deviceName: 'Counter 10', isActive: true, createdAt: '2026-05-19' };
    component.toggleDeviceActive(d);
    expect(component.toast).toBe('لا يمكن تعطيل آخر جهاز نشط');
  });
});
