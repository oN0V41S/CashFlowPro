package com.cashflow.analytics.service;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.data.redis.core.RedisTemplate;
import org.springframework.stereotype.Service;

import java.time.Duration;
import java.util.UUID;
import java.util.function.Supplier;

@Service
public class CacheService {
    private static final Logger log = LoggerFactory.getLogger(CacheService.class);
    
    private final RedisTemplate<String, Object> redisTemplate;

    @Value("${analytics.cache.ttl-minutes:60}")
    private int ttlMinutes;

    public CacheService(RedisTemplate<String, Object> redisTemplate) {
        this.redisTemplate = redisTemplate;
    }

    // Cache-Aside Pattern: Get from cache, or cache it
    public Object getOrCompute(UUID accountId, Supplier<Object> supplier){
        String key = buildKey(accountId);
        Object cached = redisTemplate.opsForValue().get(key);

        if(cached != null){
            log.debug("Cache hit for account: {}", accountId);
            return cached;
        }

        log.debug("Cache miss for account: {}", accountId);
        Object computed = supplier.get();
        redisTemplate.opsForValue().set(key, computed, Duration.ofMinutes(ttlMinutes));
        return computed;
    }

    // Invalidate individual account cache
    public void invalidate(UUID accountId){
        String key = buildKey(accountId);
        Boolean deleted = redisTemplate.delete(key);
        log.debug("Cache Invalidation {}: {}", key, deleted);
    }

    private String buildKey(UUID accountId){
        return "insights:" + accountId.toString();
    }
}
