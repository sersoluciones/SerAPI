import { TestBed } from '@angular/core/testing';
import { BaseLoginService } from './base-login.service';
describe('BaseLoginService', () => {
    let service;
    beforeEach(() => {
        TestBed.configureTestingModule({});
        service = TestBed.inject(BaseLoginService);
    });
    it('should be created', () => {
        expect(service).toBeTruthy();
    });
});
//# sourceMappingURL=base-login.service.spec.js.map