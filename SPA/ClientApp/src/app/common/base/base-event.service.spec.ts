import { TestBed } from '@angular/core/testing';

import { BaseEventService } from './base-event.service';

describe('BaseEventService', () => {
  let service: BaseEventService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(BaseEventService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
