package com.cashflow.analytics.model;

import org.junit.jupiter.api.Test;
import static org.junit.jupiter.api.Assertions.assertEquals;
import com.fasterxml.jackson.databind.ObjectMapper;
import java.math.BigDecimal;
import java.util.UUID;

public class TransferEventTest {
        private final ObjectMapper mapper = new ObjectMapper();

        @Test
        void deserializePascalCasePayloadFromCoreBanking() throws Exception {
            String json = """
                    {
                        "EventId": "99999999-9999-9999-9999-999999999999",
                        "OccurredAt": "2026-09-30T12:00:00Z",
                        "FromAccountId": "11111111-1111-1111-1111-111111111111",
                        "ToAccountId": "22222222-2222-2222-2222-222222222222",
                        "Amount": "150.50"
                    }
                    """;
            
            TransferEvent event  = mapper.readValue(json, TransferEvent.class);

            assertEquals(UUID.fromString("11111111-1111-1111-1111-111111111111"), event.fromAccountId());
            assertEquals(UUID.fromString("22222222-2222-2222-2222-222222222222"), event.toAccountId());
            assertEquals(new BigDecimal("150.50"), event.amount());
        }

        @Test 
        void deserializesCamelCasePayload() throws Exception{
            String json = """    
            {
                "fromAccountId": "11111111-1111-1111-1111-111111111111",
                "toAccountId":   "22222222-2222-2222-2222-222222222222",
                "amount":        "150.50"
            }
            """;

            TransferEvent event = mapper.readValue(json, TransferEvent.class);
            
            assertEquals(UUID.fromString("11111111-1111-1111-1111-111111111111"), event.fromAccountId());
            assertEquals(UUID.fromString("22222222-2222-2222-2222-222222222222"), event.toAccountId());
            assertEquals(new BigDecimal("150.50"), event.amount());
        }
}
