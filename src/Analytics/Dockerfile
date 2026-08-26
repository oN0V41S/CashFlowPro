# Build stage
FROM maven:3.9-eclipse-temurin-21 AS build
WORKDIR /app

COPY pom.xml .
RUN mvn dependency:go-offline -B

COPY src ./src
RUN mvn package -DskipTests -B

FROM eclipse-temurin:21-jre-alpine
WORKDIR /app

RUN addgroup -S cashflow && adduser -S cashflow -G cashflow

COPY --from=build /app/target/analytics-service-0.1.0-SNAPSHOT.jar app.jar

USER cashflow

EXPOSE 5001

ENTRYPOINT ["java", "-jar", "app.jar"]