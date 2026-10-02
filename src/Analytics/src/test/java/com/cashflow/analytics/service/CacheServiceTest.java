package com.cashflow.analytics.service;

import com.cashflow.analytics.model.InsightResponse;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.data.redis.core.RedisTemplate;
import org.springframework.data.redis.core.ValueOperations;
import org.springframework.test.util.ReflectionTestUtils;

import java.time.Duration;
import java.util.UUID;
import java.util.function.Supplier;

import static org.junit.jupiter.api.Assertions.assertSame;
import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.anyString;
import static org.mockito.Mockito.never;
import static org.mockito.Mockito.verify;
import static org.mockito.Mockito.when;
import static org.mockito.ArgumentMatchers.eq;
import static org.mockito.Mockito.mock;
import static org.mockito.Mockito.times;

@ExtendWith(MockitoExtension.class)
public class CacheServiceTest {
    @Mock 
    private RedisTemplate<String, Object> redisTemplate;

    @Mock 
    private ValueOperations<String, Object> valueOperations;

    @InjectMocks 
    private CacheService cacheService;

    private final UUID accountId = UUID.randomUUID();

    @BeforeEach 
    void setup() {
        // @Value is not processed outside Spring, so we set the field by hand
        ReflectionTestUtils.setField(cacheService, "ttlMinutes", 60);
    }

    @Test 
    void getOrCompute_returnsCachedValue_withoutCallingSupplier_onCacheHit() {
        // Arrange

        InsightResponse cached = new InsightResponse(accountId, "cached summary", "2026-10-01T10:00:00Z");
        when(redisTemplate.opsForValue()).thenReturn(valueOperations);
        when(valueOperations.get("insights:" + accountId)).thenReturn(cached);
        Supplier<InsightResponse> supplier = () -> {
            throw new AssertionError("supplier must not be called on a cache hit");
        };

        // Act
        InsightResponse result = cacheService.getOrCompute(accountId, supplier);

        // Assert
        assertSame(cached, result);
        verify(valueOperations, never()).set(anyString(), any(), any(Duration.class));
    }

    @Test 
    void getOrCompute_callsSupplierAndStoresResult_onCacheMiss() {
        // Arrange
        InsightResponse computed = new InsightResponse(accountId, "computed summary", "2026-10-01T10:00:00Z");
        when(redisTemplate.opsForValue()).thenReturn(valueOperations);
        when(valueOperations.get("insights:" + accountId)).thenReturn(null);
        Supplier<InsightResponse> supplier = () -> computed;

        // Act
        InsightResponse result = cacheService.getOrCompute(accountId, supplier);

        // Assert
        assertEquals(computed, result);
        verify(valueOperations).set(eq("insights:" + accountId), eq(computed), eq(Duration.ofMinutes(60)));
    }
}
